from flask import Blueprint, request, jsonify, render_template, send_from_directory, send_file
from flask_login import login_user, login_required, logout_user, current_user
from backend.file import *
from backend.db import mysql
from backend.llm import chain_invoke
from backend.user import Usuario
from backend.funcoes import *
from backend.projectresume import processar_e_resumir_arquivos, salvar_resumo_em_arquivo
from io import BytesIO
import os
import shutil
import zipfile


login_routes = Blueprint("login_routes", __name__)
index_routes = Blueprint("index_routes", __name__)
cadastrar_routes = Blueprint("cadastrar_routes", __name__)

thread = ""
texto = ""

# Rota para fazer login
@login_routes.route("/login", methods=["POST"])
def login():
    dados = request.get_json()
    nome_usuario = dados.get("username")
    senha = dados.get("password")

    # Consulta ao banco de dados para verificar se o usuário e a senha estão corretos
    cursor = mysql.connection.cursor()
    cursor.execute(
        "SELECT * FROM users WHERE username = %s AND password = %s",
        (nome_usuario, senha),
    )
    dados_usuario = cursor.fetchone()
    cursor.close()

    if dados_usuario:
        usuario = Usuario(usuario_id=dados_usuario[0], nome_usuario=dados_usuario[1])
        login_user(usuario)
        return jsonify({"mensagem": "Login realizado com sucesso!"})
    else:
        return jsonify({"mensagem": "Nome de usuário ou senha incorretos!"}), 401


# Rota para cadastrar um usuário
@cadastrar_routes.route("/cadastrar", methods=["POST"])
def cadastrar():
    dados = request.get_json()
    nome_usuario = dados.get("username")
    senha = dados.get("password")

    # Consulta ao banco de dados para verificar se o usuário já existe
    cursor = mysql.connection.cursor()
    cursor.execute("SELECT * FROM users WHERE username = %s", (nome_usuario,))
    dados_usuario = cursor.fetchone()

    if dados_usuario:
        cursor.close()
        return jsonify({"erro": "Nome de usuário já existe!"}), 400

    # Insere o novo usuário no banco de dados
    cursor.execute(
        "INSERT INTO users (username, password) VALUES (%s, %s)", (nome_usuario, senha)
    )
    mysql.connection.commit()
    cursor.close()

    return jsonify({"mensagem": "Usuário cadastrado com sucesso!"})


# Rota do cadastro
@cadastrar_routes.route("/cadastro")
def cadastro():
    return render_template("cadastro.html")


# Rota Inicial
@index_routes.route("/")
def index():
    return render_template("login.html")

@index_routes.route("/projetos")
def projetos():
    return render_template("projetos.html")

# Rota para fazer logout
@index_routes.route("/logout")
@login_required
def logout():
    logout_user()
    return render_template("login.html")


# Rota da home
@index_routes.route("/home")
@login_required
def home():
    nome_usuario = (
        current_user.nome_usuario
    )  # Aqui, current_user é uma variável fornecida pelo Flask-Login
    print("nome:", nome_usuario)
    chats_usuario = Usuario.obter_chats_usuario()
    return render_template(
        "index.html", username=nome_usuario, user_chats=chats_usuario
    )


# Criando rota para mandar a mensagem e receber a resposta
@index_routes.route("/get_response/<int:chat_id>", methods=["POST"])
@login_required
def get_response(chat_id):
    global texto
    data = request.get_json()
    mensagem_usuario = data.get("message")
    mensagem_usuario_inicial = mensagem_usuario
    resposta_servidor = chain_invoke(mensagem_usuario, texto,"")
    texto = ""
    # Salva a mensagem do usuário e a resposta do servidor no banco de dados
    print(resposta_servidor)
    try:
        cursor = mysql.connection.cursor()
        cursor.execute(
            "INSERT INTO messages (text_usuario, text_servidor, chat_id) VALUES (%s, %s, %s)",
            (mensagem_usuario_inicial, resposta_servidor, chat_id),
        )
        mysql.connection.commit()
        cursor.close()
    except:
        print("Erro ao salvar a mensagem no banco de dados")
    return jsonify({"message": resposta_servidor})


@index_routes.route("/add_chat", methods=["POST"])
@login_required
def add_chat():
    data = request.get_json()
    titulo = data.get("title")
    user_id = current_user.id
    global thread
    thread = "0"
    # Insere um novo chat no banco de dados
    cur = mysql.connection.cursor()
    cur.execute(
        "INSERT INTO chats (title, user_id, id_gpt) VALUES (%s, %s, %s)",
        (titulo, user_id, thread),
    )
    mysql.connection.commit()
    chat_id = cur.lastrowid
    cur.close()

    # Retorna os dados do chat recém-adicionado
    return jsonify({"chat_id": chat_id, "title": titulo})


@index_routes.route("/get_messages/<int:chat_id>")
@login_required
def get_messages(chat_id):
    global thread
    # Consulta o banco de dados para obter as mensagens correspondentes ao chat_id
    cur = mysql.connection.cursor()
    cur.execute("SELECT * FROM messages WHERE chat_id = %s", (chat_id,))
    messages = cur.fetchall()
    cur.close()

    # Formata as mensagens como uma lista de dicionários
    formatted_messages = [
        {"id": message[0], "usuario": message[1], "servidor": message[2]}
        for message in messages
    ]

    # Obtém o ID GPT do chat
    cur = mysql.connection.cursor()
    cur.execute("SELECT id_gpt FROM chats WHERE id = %s", (chat_id,))
    thread = cur.fetchone()[0]
    cur.close()

    # Retorna as mensagens como resposta JSON
    return jsonify(formatted_messages)


@index_routes.route("/delete_chat/<int:chat_id>", methods=["POST"])
@login_required
def delete_chat(chat_id):
    try:
        # Deleta todas as mensagens associadas ao chat_id
        cur = mysql.connection.cursor()
        cur.execute("DELETE FROM messages WHERE chat_id = %s", (chat_id,))

        # Deleta o chat_id
        cur.execute("DELETE FROM chats WHERE id = %s", (chat_id,))
        mysql.connection.commit()
        cur.close()

        return jsonify({"message": "Chat deletado com sucesso."}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@index_routes.route("/update_chat_title/<int:chat_id>", methods=["POST"])
@login_required
def update_chat_title(chat_id):
    try:
        data = request.get_json()
        novo_titulo = data.get("title")

        cur = mysql.connection.cursor()
        cur.execute("UPDATE chats SET title = %s WHERE id = %s", (novo_titulo, chat_id))
        mysql.connection.commit()
        cur.close()

        return jsonify({"message": "Título do chat atualizado com sucesso."}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@index_routes.route("/file-code", methods=["POST"])
@login_required
def upload_arquivo():
    if "file" not in request.files:
        return "Nenhum arquivo enviado.", 400

    arquivo = request.files["file"]

    if arquivo.filename == "":
        return "Nenhum arquivo selecionado.", 400

    # Verifica se o arquivo é permitido
    if arquivo:
        print("Arquivo salvo com sucesso")

        # print(arquivo_texto)
        extensao = arquivo.filename.rsplit(".", 1)[-1].lower()
        global texto
        file_stream = BytesIO(arquivo.read())
        texto = process_document(file_stream, extensao)
        print(texto)
        print("Arquivo enviado ao chat")

        return "Arquivo enviado com sucesso.", 200
    else:
        return "Tipo de arquivo não permitido.", 400




@index_routes.route("/file-document", methods=["POST"])
@login_required
def chat_arquivo():
    if "file" not in request.files:
        return "Nenhum arquivo enviado.", 400

    arquivo = request.files["file"]

    # Verifica se o usuário não selecionou nenhum arquivo
    if arquivo.filename == "":
        return "Nenhum arquivo selecionado.", 400

    if arquivo:
        extensao = arquivo.filename.rsplit(".", 1)[-1].lower()
        file_stream = BytesIO(arquivo.read())

        # Cria e salva o vectorstore
        verificar_e_atualizar_indice(file_stream, extensao)

        return "Arquivo enviado e vetorizado com sucesso.", 200
    else:
        return "Tipo de arquivo não permitido.", 400

@index_routes.route("/project-input", methods=["POST"])
@login_required
def project_input():
    if "file" not in request.files:
        return "Nenhum arquivo enviado.", 400

    files = request.files.getlist("file")  # Get all files from the request
    print(files)
    user_folder = os.path.join('backend', 'projects')

    if not os.path.exists(user_folder):
        os.makedirs(user_folder)
    primeiro_arquivo = True
    for arquivo in files:
        if arquivo.filename == "":
            return "Nenhum arquivo selecionado.", 400

        if arquivo:
            filename = arquivo.filename
            if primeiro_arquivo:
                output_txt_path = os.path.dirname(filename)
            file_path = os.path.join(user_folder, filename)
            
            # Create directories if they don't exist
            os.makedirs(os.path.dirname(file_path), exist_ok=True)
            print(arquivo.filename)
            arquivo.save(file_path)
            primeiro_arquivo = False
    output_txt_path = os.path.join(user_folder, output_txt_path)
    resumo_final = processar_e_resumir_arquivos(output_txt_path)
    return "Arquivos enviado e vetorizado com sucesso.", 200

@index_routes.route('/download/<filename>')
def download_file(filename):
    return send_from_directory("", filename)

@index_routes.route('/list-directory', methods=['GET'])
def list_directory():
    directory_path = "backend\projects"
    print(directory_path)
    try:
        files = os.listdir(directory_path)
        return jsonify({'files': files})
    except Exception as e:
        return jsonify({'error': str(e)})

@index_routes.route('/delete-directory', methods=['DELETE'])
def delete_directory():
    directory_name = request.args.get('directory')
    if not directory_name:
        return jsonify({'error': 'No directory specified'}), 400
    
    directory_path = os.path.join("C:/Users/muril/Music/Projeto SEC Estagio/LLM-SEC/backend/projects", directory_name)
    try:
        if os.path.isdir(directory_path):
            shutil.rmtree(directory_path)
            return jsonify({'success': True})
        else:
            return jsonify({'error': 'Directory not found'}), 404
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@index_routes.route('/download-project', methods=['GET'])
def download_project():
    directory = request.args.get('directory')
    directory_path = os.path.join('projects', directory)
    directory_path = os.path.join('backend', directory_path)
    print("DIRETÓRIO: ", directory_path)

    if not os.path.exists(directory_path):
        return jsonify(error='Diretório não encontrado'), 404

    # Cria um arquivo ZIP na memória
    memory_file = BytesIO()
    with zipfile.ZipFile(memory_file, 'w') as zf:
        for root, dirs, files in os.walk(directory_path):
            for file in files:
                file_path = os.path.join(root, file)
                arcname = os.path.relpath(file_path, directory_path)
                zf.write(file_path, arcname)

    memory_file.seek(0)
    return send_file(memory_file, mimetype='application/zip', as_attachment=True, download_name=f'{directory}.zip')