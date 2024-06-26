﻿namespace See.Sed.FichaAluno.Compatibiarra.SQL
{
    public class Query
    {

        #region SQL
        public static readonly string QueryInicioCompatibiarra = "UPDATE CADALUNOS..TB_COMPATIBILIZACAO SET FL_COMPATIBILIZACAO_EXEC = 1";

        public static readonly string QueryFinalizaCompatibiarra = "UPDATE CADALUNOS..TB_COMPATIBILIZACAO SET FL_COMPATIBILIZACAO_EXEC = 0";

        public static readonly string QueryPrepararUnidades = $@"
    -- Descobre quais são as unidades de onde tiraremos as fichas
    DROP TABLE IF EXISTS #TMP_UNIDADES_FICHAS

    SELECT ES.CD_ESCOLA
          ,UN.CD_UNIDADE
          ,ES.CD_DIRETORIA_ESTADUAL
          ,ES.CD_REDE_ENSINO
          ,MUN.CD_DNE
          ,EN.DS_LATITUDE
          ,EN.DS_LONGITUDE
          ,IIF(EXISTS (SELECT TOP 1 1 
                       FROM DB_SCE.Escola.TB_DEPENDENCIA_UNIDADE DU (NOLOCK) 
                       WHERE DU.CD_ESCOLA = ES.CD_ESCOLA 
                         AND DU.CD_UNIDADE = UN.CD_UNIDADE 
                         AND DU.CD_TP_DEPENDENCIA IN (50, 100)), 1, 0) ACESSIVEL
          ,ES.NM_COMPLETO_ESCOLA
          ,DI.NM_DIRETORIA
          ,MUN.NM_MUNICIPIO
    INTO #TMP_UNIDADES_FICHAS
    FROM DB_SCE.Escola.TB_ESCOLA ES WITH(NOLOCK)
    INNER JOIN DB_SCE.Escola.TB_UNIDADE UN WITH(NOLOCK) ON UN.CD_ESCOLA = ES.CD_ESCOLA 
                                                       AND UN.IC_UNIDADE_ATIVA = 1
    INNER JOIN DB_SCE.Escola.TB_ENDERECO EN WITH(NOLOCK) ON EN.CD_ENDERECO = UN.CD_ENDERECO
    INNER JOIN DB_SCE.Escola.TB_MUNICIPIO MUN WITH(NOLOCK) ON MUN.CD_MUNICIPIO = EN.CD_MUNICIPIO
    INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO CM WITH(NOLOCK) ON CM.DT_ANO_LETIVO = '{Program.AnoLetivo}' 
                                                                          AND CM.CD_DNE = MUN.CD_DNE 
                                                                          AND CM.FL_ATIVO = 1
    INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO_REDE_INSCRICAO CMRI WITH(NOLOCK) ON CMRI.CD_COMPATIBILIZACAO_MUNICIPIO = CM.CD_COMPATIBILIZACAO_MUNICIPIO 
                                                                                           AND CMRI.FL_ATIVO = 1 
																			               AND CMRI.CD_REDE_ENSINO = ES.CD_REDE_ENSINO
    INNER JOIN DB_SCE.Escola.TB_DIRETORIA DI WITH(NOLOCK) ON DI.CD_DIRETORIA = ES.CD_DIRETORIA_ESTADUAL
    WHERE ES.CD_SITUACAO = 1 -- Apenas escolas ativas
      AND ISNULL(ES.CD_TP_IDENTIFICADOR, 0) NOT IN(10) -- não irá compatibilizar escolas indigena
      AND ES.CD_ESCOLA NOT IN({Program.EscolasNaoCompatibiliza})
      -- VSTS: 15225 Paula Souza não participa da compat
      AND ES.CD_ESCOLA NOT IN(SELECT CD_ESCOLA FROM DB_SCE.ESCOLA.TB_ESCOLA WHERE CD_DIRETORIA=11000) 

    -- Descobre quais são as unidades/turmas que receberão alunos compatibilizados
    -- (Traz todas as escolas dos municípios, independente das turmas coletadas)

    DROP TABLE IF EXISTS #TMP_UNIDADES

    SELECT ES.CD_ESCOLA
          ,UN.CD_UNIDADE
          ,ES.CD_DIRETORIA_ESTADUAL
          ,ES.CD_REDE_ENSINO
          ,MUN.CD_DNE
          ,EN.DS_LATITUDE
          ,EN.DS_LONGITUDE
          ,IIF(EXISTS (SELECT TOP 1 1 
                        FROM DB_SCE.Escola.TB_DEPENDENCIA_UNIDADE DU (NOLOCK) 
                        WHERE DU.CD_ESCOLA = ES.CD_ESCOLA 
                          AND DU.CD_UNIDADE = UN.CD_UNIDADE 
                          AND DU.CD_TP_DEPENDENCIA IN (50, 100)), 1, 0) ACESSIVEL
         ,ES.NM_COMPLETO_ESCOLA
         ,DI.NM_DIRETORIA
         ,MUN.NM_MUNICIPIO
    INTO #TMP_UNIDADES
    FROM DB_SCE.Escola.TB_ESCOLA ES WITH(NOLOCK)
    INNER JOIN DB_SCE.Escola.TB_UNIDADE UN WITH(NOLOCK) ON UN.CD_ESCOLA = ES.CD_ESCOLA 
                                                       AND UN.IC_UNIDADE_ATIVA = 1
    INNER JOIN DB_SCE.Escola.TB_ENDERECO EN WITH(NOLOCK) ON EN.CD_ENDERECO = UN.CD_ENDERECO
    INNER JOIN DB_SCE.Escola.TB_MUNICIPIO MUN WITH(NOLOCK) ON MUN.CD_MUNICIPIO = EN.CD_MUNICIPIO
    INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO CM WITH(NOLOCK) ON CM.DT_ANO_LETIVO = '{Program.AnoLetivo}'
                                                                          AND CM.CD_DNE = MUN.CD_DNE 
                                                                          AND CM.FL_ATIVO = 1
    INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO_REDE CMR WITH(NOLOCK) ON CMR.CD_COMPATIBILIZACAO_MUNICIPIO = CM.CD_COMPATIBILIZACAO_MUNICIPIO 
                                                                                AND CMR.FL_ATIVO = 1 
																			    AND CMR.CD_REDE_ENSINO = ES.CD_REDE_ENSINO
    INNER JOIN DB_SCE.Escola.TB_DIRETORIA DI WITH(NOLOCK) ON DI.CD_DIRETORIA = ES.CD_DIRETORIA_ESTADUAL
    WHERE (ES.CD_SITUACAO = 1 OR ES.CD_ESCOLA = 573334) -- Apenas escolas ativas
    AND ISNULL(ES.CD_TP_IDENTIFICADOR, 0) NOT IN(10) -- não irá compatibilizar escolas indigena
    AND ES.CD_ESCOLA NOT IN({Program.EscolasNaoCompatibiliza})    
    -- VSTS: 15225 Paula Souza não participa da compat
    AND ES.CD_ESCOLA NOT IN(SELECT CD_ESCOLA FROM DB_SCE.ESCOLA.TB_ESCOLA WHERE CD_DIRETORIA=11000) 

    --Inclui escolas de excecao - Nova regra Liceu

	INSERT INTO #TMP_UNIDADES (CD_ESCOLA, CD_UNIDADE, CD_DIRETORIA_ESTADUAL, CD_REDE_ENSINO, CD_DNE, DS_LATITUDE, DS_LONGITUDE, ACESSIVEL, NM_COMPLETO_ESCOLA, NM_DIRETORIA, NM_MUNICIPIO)
		SELECT ES.CD_ESCOLA,
			   UN.CD_UNIDADE,
			   ES.CD_DIRETORIA_ESTADUAL,
			   EC.CD_REDE_ENSINO,
			   MU.CD_DNE,
			   EN.DS_LATITUDE,
			   EN.DS_LONGITUDE,
			   IIF(EXISTS (SELECT TOP 1 1 
                        FROM DB_SCE.Escola.TB_DEPENDENCIA_UNIDADE DU (NOLOCK) 
                        WHERE DU.CD_ESCOLA = ES.CD_ESCOLA 
                          AND DU.CD_UNIDADE = UN.CD_UNIDADE 
                          AND DU.CD_TP_DEPENDENCIA IN (50, 100)), 1, 0) ACESSIVEL,
						  ES.NM_COMPLETO_ESCOLA
         ,DI.NM_DIRETORIA
         ,MU.NM_MUNICIPIO
		FROM DB_SARA.dbo.TB_EXCECAO_COMPAT EC (NOLOCK)
			INNER JOIN DB_SCE.Escola.TB_ESCOLA ES WITH (NOLOCK)
				ON ES.CD_SITUACAO = 1
				   AND EC.CD_ESCOLA = ES.CD_ESCOLA
			INNER JOIN DB_SCE.Escola.TB_UNIDADE UN WITH (NOLOCK)
				ON UN.CD_ESCOLA = ES.CD_ESCOLA
				   AND UN.IC_UNIDADE_ATIVA = 1
			INNER JOIN DB_SCE.Escola.TB_ENDERECO EN WITH (NOLOCK)
				ON EN.CD_ENDERECO = UN.CD_ENDERECO
			INNER JOIN DB_SCE.Escola.TB_MUNICIPIO MU WITH (NOLOCK)
				ON MU.CD_MUNICIPIO = EN.CD_MUNICIPIO
				   AND MU.CD_MUNICIPIO = EN.CD_MUNICIPIO
				   AND EN.DS_LATITUDE IS NOT NULL
				   AND EN.DS_LONGITUDE IS NOT NULL
				   INNER JOIN DB_SCE.Escola.TB_DIRETORIA DI WITH(NOLOCK) ON DI.CD_DIRETORIA = ES.CD_DIRETORIA_ESTADUAL
		WHERE EC.DT_ANO_LETIVO = '{Program.AnoLetivo}'
		AND GETDATE() BETWEEN EC.DT_INI_VIG AND EC.DT_FIM_VIG
    

";

        public static readonly string QueryUnidades = @"
            SELECT TMP.CD_ESCOLA
                  ,TMP.CD_UNIDADE
                  ,TMP.CD_DIRETORIA_ESTADUAL
                  ,TMP.CD_REDE_ENSINO
                  ,TMP.CD_DNE
                  ,TMP.DS_LATITUDE
                  ,TMP.DS_LONGITUDE
                  ,TMP.ACESSIVEL
                  ,TMP.NM_COMPLETO_ESCOLA
                  ,TMP.NM_DIRETORIA
                  ,TMP.NM_MUNICIPIO
            FROM #TMP_UNIDADES TMP";

        public static readonly string QueryTurmas = $@"
    -- Traz todas as escolas/unidades/tipos de ensino das escolas dos municípios selecionados (já aplicando as regras para as turmas válidas)

    SELECT TMP.CD_ESCOLA
          ,TMP.CD_UNIDADE
          ,TMP.CD_DIRETORIA_ESTADUAL
          ,TMP.CD_REDE_ENSINO
          ,TMP.CD_DNE
          ,TMP.DS_LATITUDE
          ,TMP.DS_LONGITUDE
          ,TU.CD_TIPO_ENSINO
          ,TU.NR_SERIE
          ,TU.CD_TURMA
          ,TU.CD_DURACAO
          ,TU.CD_TIPO_CLASSE
          ,TU.CD_TURNO
          ,TU.ID_TURMA
          ,TU.NR_SALA 
          ,TU.DS_TURMA
          ,TMP.ACESSIVEL
          ,TU.ID_CAPA_FISICA_MAX
          ,TMP.NM_COMPLETO_ESCOLA
          ,TMP.NM_DIRETORIA 
          ,TMP.NM_MUNICIPIO
          ,TU.DT_INIC_AULA
          ,TU.DT_FIM_AULA
          ,TU.NR_CLASSE
          ,TU.QTD_ALUNO_ATIVO AS ALUNOS_MATRICULADOS
          ,(SELECT MAX(MA.NR_ALUNO)     
            FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) 
            WHERE MA.DT_ANO_LETIVO = '{Program.AnoLetivo}' 
            AND MA.CD_TURMA = TU.CD_TURMA 
            AND MA.FL_SITUACAO_ALUNO_CLASSE < 99) AS MAIOR_NR_ALUNO
          ,HFAP.CD_STATUS_FLUXO_APROVACAO_TURMA
    FROM #TMP_UNIDADES TMP
    LEFT JOIN DB_SARA.CADALUNOS.TB_TURMA TU WITH(NOLOCK) ON TU.DT_ANO_LETIVO = '{Program.AnoLetivo}'
                                                        AND TU.CD_ESCOLA = TMP.CD_ESCOLA
                                                        AND TU.CD_UNIDADE = TMP.CD_UNIDADE
                                                        AND TU.CD_SITUACAO = 0 -- ATIVAS
                                                        AND TU.NR_SERIE <> 0 -- NÃO CONSIDERAR MULTISSERIADAS
                                                        AND ISNULL(TU.CD_DURACAO, 0) <> 2 -- ANUAL OU 1O SEMESTRE
                                                        AND ISNULL(TU.CD_TIPO_CLASSE, 0) IN (0, 17, 18, 26, 32) -- CLASSES REGULARES, INTEGRAIS ,VENCE E APOIO COMPLEMENTAR CONCLUINTES EM
                                                        AND TU.DT_FIM_AULA >= GETDATE() -- NÃO CONSIDERAR CLASSES ENCERRADAS
	LEFT JOIN DB_SARA.CADALUNOS.TB_FLUXO_APROVACAO_TURMA FAT  WITH(NOLOCK)  ON FAT.CD_TURMA = TU.CD_TURMA
	LEFT JOIN DB_SARA.CADALUNOS.TB_HISTORICO_FLUXO_APROVACAO_TURMA HFAP WITH(NOLOCK) ON HFAP.CD_FLUXO_APROVACAO_TURMA = FAT.CD_FLUXO_APROVACAO_TURMA AND HFAP.IC_CORRENTE = 1 AND HFAP.IC_ATIVO = 1

";

        public static readonly string QueryVagasMunicipioSP = $@"
        SELECT CD_ESCOLA
              ,CD_TIPO_ENSINO
             ,NR_SERIE
             ,CD_UNIDADE
             ,VAGAS 
        FROM CADALUNOS..TB_REL_COMPAT_MUN_VAGAS (NOLOCK)
        WHERE DT_ANO_LETIVO = {Program.AnoLetivo} 
        ORDER BY CD_ESCOLA
                ,CD_UNIDADE
                ,CD_TIPO_ENSINO
                ,NR_SERIE";

        public static readonly string QueryAlunosCalculo2 = $@"
        -- Traz os dados da ficha - Utilizado nas grandes compatibilizações

        DROP TABLE IF EXISTS #TMP_FICHA

        SELECT CAST(FI.ID_ALUNO AS INT) CD_ALUNO
              ,FI.ID_FICHA_INSCRICAO
              ,CASE WHEN FI.ID_GRAU = 2 THEN 101 ELSE FI.ID_GRAU END ID_GRAU
              ,FI.ID_SERIE
              ,FI.ID_TURNO
              ,FI.ID_TURNO_NOTUR
              ,FI.FL_ESP
              ,FI.fl_e_integral
              ,TMP.CD_ESCOLA
              ,TMP.CD_UNIDADE
              ,CAST(FI.FL_FASE AS INT) FL_FASE
              ,FI.CD_MATRICULA_ALUNO_ORIGEM
              ,FI.DT_INSCR
              ,FI.CD_MOTIVO
              ,FI.CD_MATRICULA_ALUNO_ORIGEM AS CD_MATRICULA_ALUNO_ANTERIOR
              ,TU.CD_ESCOLA AS CD_ESCOLA_ANTERIOR
              ,TU.CD_UNIDADE AS CD_UNIDADE_ANTERIOR
        INTO #TMP_FICHA
        FROM #TMP_UNIDADES_FICHAS TMP
        INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI WITH(NOLOCK) ON FI.ANO_LETIVO = '{Program.AnoLetivo}'
        AND ISNULL(FI.FL_SIT_INSCR, 0) = 0
        AND ISNULL(FI.CD_MOTIVO_CANCEL, 0) = 0
        AND FI.FL_FASE IN ({Program.FasesCompatibilizacaoCalculo2})
        AND (FI.CD_MOTIVO IN ({Program.MotivoFase8}) OR '{Program.MotivoFase8}' = '0')       
        AND FI.CD_ESCOLA = TMP.CD_ESCOLA
        AND FI.CD_UNIDADE = TMP.CD_UNIDADE -- NOVO UNIDADE
        AND ISNULL(FI.CD_ESCOL_COMP_DEF, 0) = 0 -- somente tras inscrições não compatibilizadas
        AND (FI.ID_ALUNO IN ({Program.AlunosTeste}) OR '{Program.AlunosTeste}' = '0')
        LEFT JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) ON MA.CD_MATRICULA_ALUNO = FI.CD_MATRICULA_ALUNO_ORIGEM
        LEFT JOIN DB_SARA.CADALUNOS.TB_TURMA TU WITH(NOLOCK) ON TU.CD_TURMA = MA.CD_TURMA
        INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO 	CM (NOLOCK) ON  CM.DT_ANO_LETIVO = '{Program.AnoLetivo}'
																	        AND CM.CD_DNE = TMP.CD_DNE 
																	        AND CM.FL_ATIVO = 1
                                                                            AND (CM.CD_DNE = {Program.DNECompatibilizacao} OR {Program.DNECompatibilizacao} =0)
                                                                            AND (CM.CD_DNE <> {Program.NotDNECompatibilizacao} OR {Program.NotDNECompatibilizacao} =0)
        INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO_FASE CMF (NOLOCK) ON CMF.CD_COMPATIBILIZACAO_MUNICIPIO = CM.CD_COMPATIBILIZACAO_MUNICIPIO 
																                AND CMF.ID_FASE = CAST(FI.FL_FASE AS INT)
																		        AND CMF.FL_ATIVO = 1        
        -- REMOVE OS ALUNOS QUE VEM DO EJA
        DELETE TMP
        FROM #TMP_FICHA TMP
        INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA (NOLOCK) ON MA.DT_ANO_LETIVO = '{Program.AnoLetivo - 1}' 
														           AND MA.CD_MATRICULA_ALUNO = TMP.CD_MATRICULA_ALUNO_ORIGEM
                                                                   AND MA.NR_GRAU IN(3,4,5)

        SELECT TMP.CD_ALUNO, TMP.ID_FICHA_INSCRICAO, TMP.ID_GRAU, TMP.ID_SERIE, TMP.ID_TURNO, TMP.ID_TURNO_NOTUR, TMP.FL_ESP, 
               TMP.fl_e_integral, A.ID_DEFIC, EA.DS_LATITUDE, EA.DS_LONGITUDE, A.DS_LATITUDE_END_INDIC, A.DS_LONGITUDE_END_INDIC,
               TMP.CD_ESCOLA, TMP.CD_UNIDADE, A.NM_ALUNO, A.NR_RA, TMP.FL_FASE, ISNULL(EA.CD_DNE, 0) AS CD_DNE, EA.CD_ENDERECO_ALUNO, TMP.CD_MOTIVO,
               TMP.CD_MATRICULA_ALUNO_ANTERIOR, TMP.CD_ESCOLA_ANTERIOR, TMP.CD_UNIDADE_ANTERIOR
        FROM #TMP_FICHA TMP
        INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO A WITH(NOLOCK) ON A.CD_ALUNO = TMP.CD_ALUNO 
        INNER JOIN  DB_SARA.CADALUNOS.TB_ENDERECO_ALUNO EA (NOLOCK) ON EA.CD_ALUNO = TMP.CD_ALUNO AND EA.FL_CORRENTE = 1
        ORDER BY 
	    CASE WHEN TMP.FL_FASE = 8 AND ISNULL(TMP.CD_MOTIVO, 0) = 1 THEN 0 
	    WHEN TMP.FL_FASE = 0 THEN 0 
	    ELSE 1 END,
	    TMP.DT_INSCR 

";

        public static readonly string QueryAlunosContinuidadePre = $@"
    -- Traz todas as escolas/unidades/tipos de ensino de origem das escolas dos municípios selecionados (já aplicando as regras para as turmas válidas de continuidade)

    -- Primeiro, escolas/unidades/turmas de origem

    DROP TABLE IF EXISTS #TMP_TURMAS_ORIGEM

    SELECT TMP.CD_ESCOLA
          ,TMP.CD_UNIDADE
          ,TU.CD_TIPO_ENSINO
          ,TU.NR_SERIE
          ,TU.CD_TURMA
          ,TU.CD_DURACAO
          ,TU.CD_TURNO
          ,TU.ID_TURMA
          ,TU.NR_SALA
    INTO #TMP_TURMAS_ORIGEM
    FROM #TMP_UNIDADES TMP
    INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU WITH(NOLOCK) ON TU.DT_ANO_LETIVO = '{(Program.AnoLetivo - 1)}'
                                                         AND TU.CD_ESCOLA = TMP.CD_ESCOLA
                                                         AND TU.CD_UNIDADE = TMP.CD_UNIDADE
                                                         AND TU.CD_SITUACAO = 0 -- Ativas
                                                         AND TU.NR_SERIE <> 0 -- Não considerar multisseriadas
                                                         AND TU.CD_TIPO_ENSINO IN (2, 3, 4, 5, 6, 14, 30, 40, 50, 36, 37, 74, 75, 76, 78, 80, 81, 83,101)
                                                         AND ISNULL(TU.CD_DURACAO, 0) <> 1 -- Anual ou 2o semestre
                                                         AND ISNULL(TU.CD_TIPO_CLASSE, 0) IN (0, 17, 18, 22, 23, 32) -- Classes regulares, integrais, VENCE, RC, RCI
";


        public static readonly string QueryAlunosMatriculadosManual = $@"
                    SELECT DISTINCT MA.CD_ALUNO, 
	                       MA.CD_TURMA, 
	                       TU.CD_ESCOLA, 
	                       TU.CD_UNIDADE
                    FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO AS MA WITH(NOLOCK)
	                     INNER JOIN DB_SARA.CADALUNOS.TB_TURMA AS TU WITH(NOLOCK) ON TU.DT_ANO_LETIVO = '{Program.AnoLetivo}'
																                     AND TU.CD_TURMA = MA.CD_TURMA
																                     AND TU.CD_TIPO_ENSINO IN(2, 3, 4, 5, 6, 14, 30, 40, 50, 36, 37, 74, 75, 76, 78, 80, 81, 83, 101)
	                     INNER JOIN DB_SCE.Escola.TB_ESCOLA AS E(NOLOCK) ON E.CD_ESCOLA = TU.CD_ESCOLA
													                       AND E.CD_SITUACAO = 1
	                     INNER JOIN CADALUNOS..TB_PARAMETRO AS TP ON ANO_LETIVO = '{Program.AnoLetivo}'
												                    AND CD_TIPO_PARAMETRO = 21
												                    AND TP.COD_REDE_DE_ENSINO = E.CD_REDE_ENSINO
                        LEFT JOIN CADALUNOS.dbo.TB_FICHA_INSCRICAO FI (nolock) ON FI.ID_ALUNO=MA.CD_ALUNO AND FI.ANO_LETIVO= '{Program.AnoLetivo}'
						AND FI.FL_FASE = 8 AND FI.FL_SIT_INSCR = 0 AND (FI.CD_ESCOL_ALOC IS NULL OR FI.CD_ESCOL_ALOC = 0)
                    WHERE MA.DT_ANO_LETIVO = '{Program.AnoLetivo}'
	                      AND (DATEADD(d, 1, TP.DT_FIM_EXCL_MATRI_FORA_PRAZO) <= MA.DT_INCL_MATRIC
		                       OR TP.DT_FIM_EXCL_MATRI_FORA_PRAZO IS NULL)
	                      AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
	                      AND MA.FL_COMPAT_MANUAL = 1 
                          AND FI.ID_FICHA_INSCRICAO IS NULL";

        public static readonly string QueryAlunosContinuidadePos = $@"
    -- Agora traz os alunos com matrículas ativas daquelas turmas

    SELECT TMP.CD_ESCOLA
          ,TMP.CD_UNIDADE
          ,TMP.CD_TIPO_ENSINO
          ,TMP.NR_SERIE
          ,TMP.CD_TURMA
          ,TMP.CD_DURACAO
          ,TMP.CD_TURNO
          ,TMP.ID_TURMA
          ,TMP.NR_SALA
          ,MA.CD_ALUNO
          ,A.ID_DEFIC
          ,A.NM_ALUNO
          ,A.NR_RA
    FROM #TMP_TURMAS_ORIGEM TMP
    INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) ON MA.DT_ANO_LETIVO = '{(Program.AnoLetivo - 1)}'
                                                                   AND MA.CD_TURMA = TMP.CD_TURMA
                                                                   AND MA.FL_SITUACAO_ALUNO_CLASSE = 0 -- Apenas matrículas ativas
    INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO A WITH(NOLOCK) ON A.CD_ALUNO = MA.CD_ALUNO
";

        public static readonly string QueryAlunosRestantesDefinicaoInscricao = $@"
    -- Traz os dados das fichas que ainda existem

    SELECT CAST(FI.ID_ALUNO AS INT) CD_ALUNO
    FROM #TMP_UNIDADES_FICHAS TMP
    INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI WITH(NOLOCK) ON FI.ANO_LETIVO = '{Program.AnoLetivo}'
                                                            AND ISNULL(FI.FL_SIT_INSCR, 0) = 0
                                                            AND ISNULL(FI.CD_MOTIVO_CANCEL, 0) = 0
                                                            AND FI.FL_FASE IN ({Program.FasesCompatibilizacaoCalculo2})
                                                            AND FI.CD_ESCOLA = TMP.CD_ESCOLA
                                                            AND FI.CD_UNIDADE = TMP.CD_UNIDADE -- NOVO UNIDADE
";

        public static readonly string QueryAlunosRestantesContinuidade = $@"
    -- Agora traz os alunos com matrículas ativas das turmas de continuidade

    SELECT MA.CD_ALUNO
    FROM #TMP_TURMAS_ORIGEM TMP
    INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) ON MA.DT_ANO_LETIVO = '{(Program.AnoLetivo - 1)}'
                                                                   AND MA.CD_TURMA = TMP.CD_TURMA
                                                                   AND MA.FL_SITUACAO_ALUNO_CLASSE = 0 -- Apenas matrículas ativas
";

        public static readonly string QueryIrmaos = $@"
    SELECT CD_ALUNO 
          ,CD_IRMAO
          ,FL_GEMEO 
    FROM DB_SARA.CADALUNOS.TB_ALUNO_IRMAO WITH(NOLOCK) ";

        public static readonly string QueryIrmaosSemRodada = $@"
    SELECT MA.CD_ALUNO
          ,TU.CD_ESCOLA
          ,TU.CD_UNIDADE
          ,TU.CD_TURMA
          ,A.ID_DEFIC 
    FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK)
    INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO A WITH(NOLOCK) ON A.CD_ALUNO = MA.CD_ALUNO
    INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU WITH(NOLOCK) ON TU.DT_ANO_LETIVO = '{Program.AnoLetivo}'
                                                         AND TU.CD_TURMA = MA.CD_TURMA
                                                         AND TU.CD_SITUACAO = 0
    WHERE MA.DT_ANO_LETIVO = '{Program.AnoLetivo}'
     AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
     AND MA.CD_ALUNO IN (SELECT I.CD_IRMAO
                        FROM #TMP_FICHA TMP 
                        INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO_IRMAO I WITH(NOLOCK) ON I.CD_ALUNO = TMP.CD_ALUNO)
";


        public static readonly string QueryAtualizaTurmaQtds = $@"
    UPDATE TU SET
    TU.QTD_ALUNO_DIGIT           = (SELECT COUNT(1) FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) WHERE MA.DT_ANO_LETIVO = '{Program.AnoLetivo}' AND MA.CD_TURMA = TU.CD_TURMA AND MA.FL_SITUACAO_ALUNO_CLASSE < 99),
    TU.QTD_ALUNO_ATIVO			 = (SELECT COUNT(1) FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) WHERE MA.DT_ANO_LETIVO = '{Program.AnoLetivo}' AND MA.CD_TURMA = TU.CD_TURMA AND MA.FL_SITUACAO_ALUNO_CLASSE = 0),
    TU.QTD_ALUNO_TRANSF_CLASSE   = (SELECT COUNT(1) FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) WHERE MA.DT_ANO_LETIVO = '{Program.AnoLetivo}' AND MA.CD_TURMA = TU.CD_TURMA AND MA.FL_SITUACAO_ALUNO_CLASSE = 1)
     FROM DB_SARA.CADALUNOS.TB_TURMA TU
        WHERE TU.DT_ANO_LETIVO = '{Program.AnoLetivo}'
";


        public static readonly string GuardaPosicaoVagas = $@"INSERT INTO CADALUNOS..TB_REL_COMPAT_MUN_VAGAS_HIST (DT_PROCESSAMENTO, DT_ANO_LETIVO, CD_ESCOLA, CD_TIPO_ENSINO, NR_SERIE, CD_UNIDADE, TURMAS, TOTAL_VAGAS, VAGAS)
                                                        SELECT GETDATE(), DT_ANO_LETIVO, CD_ESCOLA, CD_TIPO_ENSINO, NR_SERIE, CD_UNIDADE, TURMAS, TOTAL_VAGAS, VAGAS FROM CADALUNOS..TB_REL_COMPAT_MUN_VAGAS WHERE DT_ANO_LETIVO = {Program.AnoLetivo}";

        public static readonly string QueryLimpeza = $@"
    DROP TABLE IF EXISTS #TMP_TURMAS_ORIGEM
    DROP TABLE IF EXISTS #TMP_FICHA
    DROP TABLE IF EXISTS #TMP_UNIDADES
    DROP TABLE IF EXISTS #TMP_UNIDADES_FICHAS
";

        public static readonly string QueryAlunoEscolaDistancia = $@"
      SELECT  R.ID_REL_COMPAT_ROTA_APE
            , R.CD_ALUNO
            , R.CD_ESCOLA
            , R.CD_UNIDADE
            , R.NR_DISTANCIA_CAMINHANDO 
            , R.FL_CONTINUIDADE
            , R.CD_ENDERECO_ALUNO
    FROM CALCULO_ROTAS.DBO.TB_REL_COMPAT_ROTA_APE R (NOLOCK)
    inner join db_sce.escola.tb_escola e (nolock) ON R.CD_ESCOLA=E.CD_ESCOLA
    inner join db_sce.escola.tb_unidade u (nolock) on u.cd_escola=e.cd_escola AND U.CD_UNIDADE = R.CD_UNIDADE and u.ic_unidade_ativa=1
    inner join db_sce.escola.tb_endereco en (nolock) on en.cd_endereco=u.cd_endereco
    inner join db_sce.escola.tb_municipio m (nolock) on m.cd_municipio = en.cd_municipio
    WHERE R.FL_DISTANCIA_APE = 1 
      AND R.FL_COMPATIBILIZA = 1
      AND (
      (R.NR_DISTANCIA_CAMINHANDO < 2001 AND E.CD_REDE_ENSINO <> 2) OR 
      (R.NR_DISTANCIA_CAMINHANDO < 1501 AND E.CD_REDE_ENSINO = 2 AND m.CD_DNE = 9668) OR 
      (R.NR_DISTANCIA_CAMINHANDO < 2001 AND E.CD_REDE_ENSINO = 2 AND m.CD_DNE <> 9668)
      )
      AND (R.CD_ALUNO IN ({Program.AlunosTeste}) OR '{Program.AlunosTeste}' = '0')
    ORDER BY R.NR_DISTANCIA_CAMINHANDO

";

        public static readonly string QueryAlunoEscolaDistanciaSemRodadaAPENASAPE = $@"
select CD_ROTA_APE_ANUAL 
            , CD_ALUNO 
            , CD_ESCOLA 
            , CD_UNIDADE 
            , NR_DISTANCIA_CAMINHANDO  
            , FL_CONTINUIDADE 
            , CD_ENDERECO_ALUNO
from (
      SELECT  R.CD_ROTA_APE_ANUAL
            , R.CD_ALUNO
            , R.CD_ESCOLA
            , R.CD_UNIDADE
            , R.NR_DISTANCIA_CAMINHANDO 
            , R.FL_CONTINUIDADE
            , R.CD_ENDERECO_ALUNO
    FROM CALCULO_ROTAS.DBO.TB_ROTA_APE_ANUAL R (NOLOCK)
    inner join db_sce.escola.tb_escola e (nolock) ON R.CD_ESCOLA=E.CD_ESCOLA
	inner join db_sce.escola.tb_unidade u (nolock) on u.cd_escola=e.cd_escola AND U.CD_UNIDADE = R.CD_UNIDADE and u.ic_unidade_ativa=1
	inner join db_sce.escola.tb_endereco en (nolock) on en.cd_endereco=u.cd_endereco
	inner join db_sce.escola.tb_municipio m (nolock) on m.cd_municipio = en.cd_municipio
    WHERE R.FL_DISTANCIA_APE = 1 
      AND R.FL_COMPATIBILIZA = 1
      AND (
          (R.NR_DISTANCIA_CAMINHANDO < 2001 AND E.CD_REDE_ENSINO <> 2) OR 
          (R.NR_DISTANCIA_CAMINHANDO < 1501 AND E.CD_REDE_ENSINO = 2 AND m.CD_DNE = 9668) OR
          (R.NR_DISTANCIA_CAMINHANDO < 2001 AND E.CD_REDE_ENSINO = 2 AND m.CD_DNE <> 9668)
        )
      AND R.DT_ANO_LETIVO = '{Program.AnoLetivo}'
      AND (R.CD_DNE_ALUNO <> {Program.NotDNECompatibilizacao} OR {Program.NotDNECompatibilizacao} =0)
      AND (R.CD_DNE_ALUNO = {Program.DNECompatibilizacao} OR {Program.DNECompatibilizacao} =0)
      AND (R.CD_ALUNO IN ({ Program.AlunosTeste}) OR '{Program.AlunosTeste}' = '0')

UNION                    

        SELECT cast((ROW_NUMBER() OVER(ORDER BY MA.CD_ALUNO ASC))+10000000 as bigint) as CD_ROTA_APE_ANUAL
        , MA.CD_ALUNO
        , TU.CD_ESCOLA
        , TU.CD_UNIDADE
        , 9999 NR_DISTANCIA_CAMINHANDO 
        , cast(1 as bit) FL_CONTINUIDADE
        , EA.CD_ENDERECO_ALUNO
        from DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA (nolock)
        INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU (nolock) ON MA.CD_TURMA=TU.CD_TURMA
        and TU.TP_REDE_ENSINO = 1
        inner join CADALUNOS.DBO.TB_SERIE_TIPO_ENSINO_ESCOLA_ANUAL STE (nolock) ON STE.CD_ESCOLA=TU.CD_ESCOLA and STE.CD_UNIDADE=TU.CD_UNIDADE
        INNER JOIN  DB_SARA.CADALUNOS.TB_ENDERECO_ALUNO EA (NOLOCK) ON MA.CD_ALUNO = EA.CD_ALUNO
                                                                       AND EA.FL_ATIVO = 1 
                                                                       AND EA.FL_CORRENTE = 1

        WHERE TU.DT_ANO_LETIVO = '{Program.AnoLetivo-1}'
        AND MA.DT_ANO_LETIVO='{Program.AnoLetivo - 1}'
        AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
        and tu.cd_tipo_ensino=14
        and tu.nr_serie=9
        and ste.cd_tipo_ensino=101
        and ste.nr_serie=1
        AND (MA.CD_ALUNO IN ({Program.AlunosTeste}) OR '{Program.AlunosTeste}' = '0')

UNION 
        SELECT cast((ROW_NUMBER() OVER(ORDER BY MA.CD_ALUNO ASC))+100000000 as bigint) as CD_ROTA_APE_ANUAL
        , MA.CD_ALUNO
        , TU.CD_ESCOLA
        , TU.CD_UNIDADE
        , 9999 NR_DISTANCIA_CAMINHANDO 
        , cast(1 as bit) FL_CONTINUIDADE
        , EA.CD_ENDERECO_ALUNO
        from DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA (nolock)
        INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU (nolock) ON MA.CD_TURMA=TU.CD_TURMA
        and TU.TP_REDE_ENSINO = 1
        inner join CADALUNOS.DBO.TB_SERIE_TIPO_ENSINO_ESCOLA_ANUAL STE (nolock) ON STE.CD_ESCOLA=TU.CD_ESCOLA and STE.CD_UNIDADE=TU.CD_UNIDADE
        INNER JOIN  DB_SARA.CADALUNOS.TB_ENDERECO_ALUNO EA (NOLOCK) ON MA.CD_ALUNO = EA.CD_ALUNO
                                                                       AND EA.FL_ATIVO = 1 
                                                                       AND EA.FL_CORRENTE = 1

        WHERE TU.DT_ANO_LETIVO = '{Program.AnoLetivo - 1}'
        AND MA.DT_ANO_LETIVO='{Program.AnoLetivo - 1}'
        AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
        and tu.cd_tipo_ensino=14
        and tu.nr_serie=5
        and ste.cd_tipo_ensino=14
        and ste.nr_serie=6
        AND (MA.CD_ALUNO IN ({Program.AlunosTeste}) OR '{Program.AlunosTeste}' = '0')

-------- Habilitar apenas se rodar script de definição de continuidade
UNION 
        SELECT cast((ROW_NUMBER() OVER(ORDER BY MA.CD_ALUNO ASC))+190000000 as bigint) as CD_ROTA_APE_ANUAL
        , MA.CD_ALUNO
        , TU.CD_ESCOLA
        , TU.CD_UNIDADE
        , 9999 NR_DISTANCIA_CAMINHANDO 
        , cast(1 as bit) FL_CONTINUIDADE
        , EA.CD_ENDERECO_ALUNO
        from DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA (nolock)
        INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU (nolock) ON MA.CD_TURMA=TU.CD_TURMA
        and TU.TP_REDE_ENSINO = 1
        inner join CADALUNOS.DBO.TB_SERIE_TIPO_ENSINO_ESCOLA_ANUAL STE (nolock) ON STE.CD_ESCOLA=TU.CD_ESCOLA and STE.CD_UNIDADE=TU.CD_UNIDADE
        INNER JOIN  DB_SARA.CADALUNOS.TB_ENDERECO_ALUNO EA (NOLOCK) ON MA.CD_ALUNO = EA.CD_ALUNO
                                                                       AND EA.FL_ATIVO = 1 
                                                                       AND EA.FL_CORRENTE = 1

        WHERE TU.DT_ANO_LETIVO = '{Program.AnoLetivo - 1}'
        AND MA.DT_ANO_LETIVO='{Program.AnoLetivo - 1}'
        AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
        and 
		(
			(tu.cd_tipo_ensino=14
			and tu.nr_serie in (2,3,4,6,7,8)
			and ste.cd_tipo_ensino=14
			and ste.nr_serie in (3,4,5,6,7,8,9))
			OR
			(tu.cd_tipo_ensino=101
			and tu.nr_serie in (2)
			and ste.cd_tipo_ensino=101
			and ste.nr_serie IN (3))
		)
        AND (MA.CD_ALUNO IN ({Program.AlunosTeste}) OR '{Program.AlunosTeste}' = '0')

)
AS T ORDER BY NR_DISTANCIA_CAMINHANDO

";


        public static readonly string QueryAlunoEscolaDistanciaSemRodada = $@"
        
SELECT CAST(CD_ROTA_APE_ANUAL AS BIGINT) AS      CD_ROTA_APE_ANUAL, 
	   CD_ALUNO, 
	   CD_ESCOLA, 
	   CD_UNIDADE, 
	   CAST(NR_DISTANCIA_CAMINHANDO AS FLOAT) AS NR_DISTANCIA_CAMINHANDO, 
	   CAST(FL_CONTINUIDADE AS BIT) AS           FL_CONTINUIDADE, 
	   CD_ENDERECO_ALUNO, 
	   APE
FROM
(
	SELECT R.CD_ROTA_APE_ANUAL, 
		   R.CD_ALUNO, 
		   R.CD_ESCOLA, 
		   R.CD_UNIDADE, 
		   R.NR_DISTANCIA_CAMINHANDO, 
		   R.FL_CONTINUIDADE, 
		   R.CD_ENDERECO_ALUNO, 
		   0 AS APE
	FROM CALCULO_ROTAS.DBO.TB_ROTA_APE_ANUAL AS R(NOLOCK)
    inner join db_sce.escola.tb_escola e (nolock) ON R.CD_ESCOLA=E.CD_ESCOLA
	inner join db_sce.escola.tb_unidade u (nolock) on u.cd_escola=e.cd_escola AND U.CD_UNIDADE = R.CD_UNIDADE and u.ic_unidade_ativa=1
	inner join db_sce.escola.tb_endereco en (nolock) on en.cd_endereco=u.cd_endereco
	inner join db_sce.escola.tb_municipio m (nolock) on m.cd_municipio = en.cd_municipio
	WHERE R.FL_DISTANCIA_APE = 1
		  AND R.FL_COMPATIBILIZA = 1
		  AND (
        (R.NR_DISTANCIA_CAMINHANDO < 2001 AND E.CD_REDE_ENSINO <> 2) OR 
        (R.NR_DISTANCIA_CAMINHANDO < 1501 AND E.CD_REDE_ENSINO = 2 AND m.CD_DNE = 9668) OR
        (R.NR_DISTANCIA_CAMINHANDO < 2001 AND E.CD_REDE_ENSINO = 2 AND m.CD_DNE <> 9668)
        )
          AND R.DT_ANO_LETIVO = '{Program.AnoLetivo}'
          AND (R.CD_DNE_ALUNO <> {Program.NotDNECompatibilizacao} OR {Program.NotDNECompatibilizacao} =0)
          AND (R.CD_DNE_ALUNO = {Program.DNECompatibilizacao} OR {Program.DNECompatibilizacao} =0)
	
    UNION ALL
	SELECT CAST(R.CD_ROTA_LINEAR_ANUAL AS BIGINT) AS CD_ROTA_APE_ANUAL, 
		   R.CD_ALUNO, 
		   R.CD_ESCOLA, 
		   R.CD_UNIDADE, 
		   CAST(DS_DISTANCIA * 1000.00 AS FLOAT) AS  NR_DISTANCIA_CAMINHANDO, 
		   CAST(
	(
		SELECT TOP 1 1
		FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO AS MA WITH(NOLOCK)
		WHERE DT_ANO_LETIVO = '{Program.AnoLetivo}'
			  AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
			  AND MA.NR_GRAU IN(2, 101, 14)
	) AS BIT) AS                                     FL_CONTINUIDADE, 
		   R.CD_ENDERECO_ALUNO, 
		   1 AS                                      APE
	FROM CALCULO_ROTAS.DBO.TB_ROTA_LINEAR_ANUAL AS R(NOLOCK)
		 INNER JOIN CADALUNOS.dbo.TB_FICHA_INSCRICAO AS FI(NOLOCK) ON R.CD_ALUNO = FI.ID_ALUNO
																	  AND FI.ANO_LETIVO = '{Program.AnoLetivo}'
                                                                      AND FI.FL_FASE IN({Program.FasesCompatibilizacaoCalculo2})
																	  AND (FI.CD_MOTIVO IN ({Program.MotivoFase8}) OR '{Program.MotivoFase8}' = '0')                                                                      
               
	WHERE DS_DISTANCIA * 1000 < 1300 

) AS T
WHERE T.NR_DISTANCIA_CAMINHANDO < 2001
ORDER BY APE, 
		 NR_DISTANCIA_CAMINHANDO";
        #endregion

        #region Rodadas
        public static readonly string QueryAlunosRodadasEtapa1 = $@"
-- **********************************************************************************
--
-- PARA AS RODADAS, IGNORAR A HISTORIA DO 6 ANO DO MUNICÍPIO DE SP!
--
-- **********************************************************************************

-- **********************************************************************************
--  Etapa 1 (Alunos fora da rede)
-- **********************************************************************************

    DROP TABLE IF EXISTS #TMP_FICHA

    SELECT DISTINCT CAST(FI.ID_ALUNO AS INT) CD_ALUNO
                   ,FI.ID_FICHA_INSCRICAO
                   ,CASE WHEN FI.ID_GRAU = 2 AND FI.ID_SERIE IN (1, 2) THEN 101 ELSE FI.ID_GRAU END ID_GRAU
                   ,FI.ID_SERIE
                   ,FI.ID_TURNO
                   ,FI.ID_TURNO_NOTUR
                   ,FI.FL_ESP
                   ,FI.fl_e_integral
                   ,TMP.CD_ESCOLA
                   ,TMP.CD_UNIDADE
                   ,CAST(FI.FL_FASE AS INT) FL_FASE
                   ,FI.ID_GRAU_EQUIVALENTE
                   ,FI.ID_SERIE_EQUIVALENTE
                   ,CAST(0 AS BIGINT) CD_MATRICULA_ATENDIDA
                   ,ISNULL(FI.ID_SIT_COMP_DEF, 0) AS ID_SIT_COMP_DEF
                   ,FI.CD_MOTIVO
    INTO #TMP_FICHA
    FROM #TMP_UNIDADES_FICHAS TMP
    INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI WITH(NOLOCK) ON FI.ANO_LETIVO = '{Program.AnoLetivo}'
														    AND ISNULL(FI.FL_SIT_INSCR, 0) = 0
														    AND ISNULL(FI.CD_MOTIVO_CANCEL, 0) = 0
														    {(Program.ProcessarInscricoesForaRedeNasRodadas ? "AND FI.FL_FASE IN (4, 7)" : "AND FI.FL_FASE = 99999") /* Se não for para processar o Inscrições de fora da Rede, basta utilizar uma fase inexistente, que não trará alunos! */}
														    AND FI.CD_ESCOLA = TMP.CD_ESCOLA
														    AND FI.CD_UNIDADE = TMP.CD_UNIDADE -- NOVO UNIDADE
														    AND ISNULL(FI.CD_ESCOL_COMP_DEF, 0) = 0
														    AND FI.ID_GRAU IN (2, 6, 14, 78, 80, 81, 82, 83, 84, 101)
                                                            AND (FI.ID_ALUNO IN ({Program.AlunosTeste}) OR '{Program.AlunosTeste}' = '0')
    {(Program.UtilizarDataLimiteMinimaDasFichasDeInscricaoNasRodadas ? "AND FI.DT_INCL >= " + Program.DataLimiteMinimaDasFichasDeInscricaoNasRodadas : "")}
    {(Program.UtilizarDataLimiteMaximaDasFichasDeInscricaoNasRodadas ? "AND FI.DT_INCL <= " + Program.DataLimiteMaximaDasFichasDeInscricaoNasRodadas : "")}
    INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO 		CM (NOLOCK)  ON CM.DT_ANO_LETIVO = '{Program.AnoLetivo}'
                                                                            AND CM.CD_DNE = TMP.CD_DNE 
																	        AND CM.FL_ATIVO = 1
                                                                            AND (CM.CD_DNE = {Program.DNECompatibilizacao} OR {Program.DNECompatibilizacao} =0)
                                                                            AND (CM.CD_DNE <> {Program.NotDNECompatibilizacao} OR {Program.NotDNECompatibilizacao} =0)
    INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO_FASE CMF (NOLOCK) ON CMF.CD_COMPATIBILIZACAO_MUNICIPIO = CM.CD_COMPATIBILIZACAO_MUNICIPIO 
																            AND CMF.ID_FASE = CAST(FI.FL_FASE AS INT)
																		    AND CMF.FL_ATIVO = 1

    DROP TABLE IF EXISTS #TMP_MA

    SELECT MA.CD_MATRICULA_ALUNO
          ,MA.DT_INCL
          ,MA.CD_ALUNO
          ,MA.NR_GRAU
          ,MA.NR_SERIE
          ,MA.CD_TURMA
          ,EQ.CD_TIPO_ENSINO_EQUIVALENTE
          ,EQ.NR_SERIE_EQUIVALENTE
    INTO #TMP_MA
    FROM #TMP_FICHA TMP
    INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) ON MA.DT_ANO_LETIVO = '{Program.AnoLetivo}'
                                                                   AND MA.CD_ALUNO = TMP.CD_ALUNO
                                                                   AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
    INNER JOIN CADALUNOS..TB_TP_ENSINO_EQUIVALENCIA EQ WITH(NOLOCK) ON EQ.CD_TIPO_ENSINO = MA.NR_GRAU 
                                                                   AND EQ.NR_SERIE = MA.NR_SERIE
                                                                   AND EQ.DT_EXCL IS NULL
    INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU WITH(NOLOCK) ON TU.DT_ANO_LETIVO = '{Program.AnoLetivo}'
                                                         AND TU.CD_SITUACAO = 0
                                                         AND TU.CD_TURMA = MA.CD_TURMA
    INNER JOIN DB_SCE.Escola.TB_ESCOLA ES WITH(NOLOCK) ON ES.CD_ESCOLA = TU.CD_ESCOLA
                                                      AND ES.CD_SITUACAO = 1
                                                      AND ES.CD_REDE_ENSINO IN (1, 2)


    UPDATE TMPFI SET TMPFI.CD_MATRICULA_ATENDIDA = TMPMA.CD_MATRICULA_ALUNO
    FROM #TMP_FICHA TMPFI
    INNER JOIN #TMP_MA TMPMA ON TMPMA.CD_ALUNO = TMPFI.CD_ALUNO
						    AND TMPMA.CD_TIPO_ENSINO_EQUIVALENTE = TMPFI.ID_GRAU_EQUIVALENTE
						    AND TMPMA.NR_SERIE_EQUIVALENTE = TMPFI.ID_SERIE_EQUIVALENTE

    SELECT TMP.CD_ALUNO
          ,TMP.ID_FICHA_INSCRICAO
          ,TMP.ID_GRAU
          ,TMP.ID_SERIE
          ,TMP.ID_TURNO
          ,TMP.ID_TURNO_NOTUR
          ,TMP.FL_ESP
          ,TMP.fl_e_integral
          ,A.ID_DEFIC
          ,EA.DS_LATITUDE
          ,EA.DS_LONGITUDE
          ,A.DS_LATITUDE_END_INDIC
          ,A.DS_LONGITUDE_END_INDIC
          ,TMP.CD_ESCOLA
          ,TMP.CD_UNIDADE
          ,A.NM_ALUNO
          ,A.NR_RA
          ,TMP.FL_FASE
          ,ISNULL(EA.CD_DNE, 0) AS CD_DNE, EA.CD_ENDERECO_ALUNO
          ,TMP.CD_MOTIVO
    FROM #TMP_FICHA TMP
    INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO A WITH(NOLOCK) ON A.CD_ALUNO = TMP.CD_ALUNO
    INNER JOIN  DB_SARA.CADALUNOS.TB_ENDERECO_ALUNO EA (NOLOCK) ON EA.CD_ALUNO = TMP.CD_ALUNO 
                                                               AND EA.FL_ATIVO = 1 
                                                               AND EA.FL_CORRENTE = 1
    WHERE TMP.CD_MATRICULA_ATENDIDA = 0

    DROP TABLE IF EXISTS #TMP_FICHA
    DROP TABLE IF EXISTS #TMP_MA
";

        public static readonly string QueryAlunosRodadasEtapa2 = $@"
 -- **********************************************************************************
--  Etapa 2 (Alunos de deslocamento)
--
--  Primeiro rodar todos com alteração de endereço
--  ((FI.FL_FASE = 8 e FI.CD_MOTIVO = 1) ou FI.FL_FASE = 0)
--  Depois, os sem alteração de endereço, ordenados pelo FI.DT_INCL
-- **********************************************************************************

    DROP TABLE IF EXISTS #TMP_FICHA

    SELECT DISTINCT CAST(FI.ID_ALUNO AS INT) CD_ALUNO
                   ,FI.ID_FICHA_INSCRICAO
                   ,CASE WHEN FI.ID_GRAU = 2 AND FI.ID_SERIE IN (1, 2) THEN 101 ELSE FI.ID_GRAU END ID_GRAU
                   ,FI.ID_SERIE
                   ,FI.ID_TURNO
                   ,FI.ID_TURNO_NOTUR
                   ,FI.FL_ESP
                   ,FI.fl_e_integral
                   ,TMP.CD_ESCOLA
                   ,TMP.CD_UNIDADE
                   ,CAST(FI.FL_FASE AS INT) AS FL_FASE
                   ,FI.DT_INCL         
                   ,FI.ID_GRAU_EQUIVALENTE
                   ,FI.ID_SERIE_EQUIVALENTE
                   ,CAST(FI.CD_MATRICULA_ALUNO_ORIGEM AS BIGINT) CD_MATRICULA_ALUNO_ANTERIOR
                   ,CAST(TU.CD_ESCOLA AS INT) AS CD_ESCOLA_ANTERIOR
                   ,CAST(TU.CD_UNIDADE AS INT) AS CD_UNIDADE_ANTERIOR 
                   ,ISNULL(FI.ID_SIT_COMP_DEF, 0) AS ID_SIT_COMP_DEF
                   ,FI.CD_MOTIVO
    INTO #TMP_FICHA
    FROM #TMP_UNIDADES_FICHAS TMP
    INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI WITH(NOLOCK) ON FI.ANO_LETIVO = '{Program.AnoLetivo}'
                                                            AND ISNULL(FI.FL_SIT_INSCR, 0) = 0
                                                            AND ISNULL(FI.CD_MOTIVO_CANCEL, 0) = 0
                                                            {(Program.ProcessarDeslocamentoNasRodadas ? "AND FI.FL_FASE IN (0, 8, 9)" : "AND FI.FL_FASE = 99999") /* Se não for para processar o deslocamento, basta utilizar uma fase inexistente, que não trará alunos! */}
                                                            AND FI.CD_ESCOLA = TMP.CD_ESCOLA
                                                            AND FI.CD_UNIDADE = TMP.CD_UNIDADE -- NOVO UNIDADE
                                                            AND ISNULL(FI.CD_ESCOL_COMP_DEF, 0) = 0
                                                            --AND FI.ID_GRAU NOT IN (3, 4, 5, 37, 46, 47, 61, 62, 63, 74, 75) -- NÃO DEVEMOS COMPATIBILIZAR OS EJA!
                                                            AND FI.ID_GRAU IN (2, 14, 78, 80, 81, 82, 83, 84, 101)
                                                            AND (FI.ID_ALUNO IN ({Program.AlunosTeste}) OR '{Program.AlunosTeste}' = '0')
    INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO CM (NOLOCK)  ON CM.DT_ANO_LETIVO = '{Program.AnoLetivo}'
                                                                       AND CM.CD_DNE = TMP.CD_DNE 
																	   AND CM.FL_ATIVO = 1
                                                                       AND (CM.CD_DNE = {Program.DNECompatibilizacao} OR {Program.DNECompatibilizacao} =0)
                                                                       AND (CM.CD_DNE <> {Program.NotDNECompatibilizacao} OR {Program.NotDNECompatibilizacao} =0)
    INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO_FASE CMF (NOLOCK) ON CMF.CD_COMPATIBILIZACAO_MUNICIPIO = CM.CD_COMPATIBILIZACAO_MUNICIPIO 
																            AND CMF.ID_FASE = CAST(FI.FL_FASE AS INT)
																		    AND CMF.FL_ATIVO = 1
    {(Program.UtilizarDataLimiteMinimaDasFichasDeInscricaoNasRodadas ? "AND FI.DT_INCL >= " + Program.DataLimiteMinimaDasFichasDeInscricaoNasRodadas : "")}
    {(Program.UtilizarDataLimiteMaximaDasFichasDeInscricaoNasRodadas ? "AND FI.DT_INCL <= " + Program.DataLimiteMaximaDasFichasDeInscricaoNasRodadas : "")}
    -- pega os dados da matricula de origem
    INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA(NOLOCK) ON MA.DT_ANO_LETIVO = '{Program.AnoLetivo}' 
														      AND MA.CD_MATRICULA_ALUNO = FI.CD_MATRICULA_ALUNO_ORIGEM 
														      AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
    INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU (NOLOCK) ON TU.DT_ANO_LETIVO ='{Program.AnoLetivo}' 
												     AND TU.CD_TURMA = MA.CD_TURMA

    SELECT TMP.CD_ALUNO
          ,TMP.ID_FICHA_INSCRICAO
          ,TMP.ID_GRAU
          ,TMP.ID_SERIE
          ,TMP.ID_TURNO
          ,TMP.ID_TURNO_NOTUR
          ,TMP.FL_ESP
          ,TMP.fl_e_integral
          ,A.ID_DEFIC
          ,EA.DS_LATITUDE
          ,EA.DS_LONGITUDE
          ,A.DS_LATITUDE_END_INDIC
          ,A.DS_LONGITUDE_END_INDIC
          ,TMP.CD_ESCOLA
          ,TMP.CD_UNIDADE
          ,A.NM_ALUNO
          ,A.NR_RA
          ,TMP.FL_FASE    
          ,TMP.CD_MATRICULA_ALUNO_ANTERIOR
          ,TMP.CD_ESCOLA_ANTERIOR
          ,TMP.CD_UNIDADE_ANTERIOR
          ,ISNULL(EA.CD_DNE, 0) AS CD_DNE,  EA.CD_ENDERECO_ALUNO
          ,TMP.CD_MOTIVO
    FROM #TMP_FICHA TMP
    INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO A WITH(NOLOCK) ON A.CD_ALUNO = TMP.CD_ALUNO
    INNER JOIN  DB_SARA.CADALUNOS.TB_ENDERECO_ALUNO EA (NOLOCK) ON EA.CD_ALUNO = TMP.CD_ALUNO 
                                                               AND EA.FL_ATIVO = 1
                                                               AND EA.FL_CORRENTE = 1
    WHERE TMP.CD_MATRICULA_ALUNO_ANTERIOR > 0 
    ORDER BY CASE WHEN TMP.FL_FASE = 8 AND ISNULL(TMP.CD_MOTIVO, 0) = 1 THEN 0 WHEN TMP.FL_FASE = 0 THEN 0 ELSE 1 END ASC, TMP.DT_INCL ASC

    DROP TABLE IF EXISTS #TMP_FICHA
";

        public static readonly string QueryTurmasIrmaosRodadas = $@"
    SELECT MA.CD_ALUNO
          ,TU.CD_ESCOLA
          ,TU.CD_UNIDADE
          ,TU.CD_TURMA
          ,A.ID_DEFIC FROM
    DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK)
    INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO A WITH(NOLOCK) ON A.CD_ALUNO = MA.CD_ALUNO
    INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU WITH(NOLOCK) ON TU.DT_ANO_LETIVO = '{Program.AnoLetivo}'
                                                         AND TU.CD_TURMA = MA.CD_TURMA
                                                         AND TU.CD_SITUACAO = 0
    WHERE MA.DT_ANO_LETIVO = '{Program.AnoLetivo}'
      AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
      AND MA.CD_ALUNO IN (
";
        #endregion

        #region SQL MATRICULA E INSCRICAO ANO SEGUINTE
        public static readonly string QueryMatInscAnoSeguinte = @"
    --DERRUBA MATRICULA DOS ALUNOS PARA O ANO SEGUINTE
    UPDATE MA SET
    	MA.FL_SITUACAO_ALUNO_CLASSE = 99,
    	MA.DT_EXCL			        = GETDATE(),
    	MA.LOGIN_EXCL			    = '[AUTO COMPAT]',
    	MA.MACHINE_EXCL             = '[AUTO COMPAT]',
    	MA.USER_EXCL                = '[AUTO COMPAT]'
    FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL I (NOLOCK)
    INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA(NOLOCK) ON MA.DT_ANO_LETIVO = @DT_ANO_LETIVO_ANO_SEGUINTE AND MA.CD_ALUNO = I.CD_ALUNO AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
    WHERE I.ID_RODADA = @ID_RODADA
    AND I.CD_MATRICULA_ALUNO > 0

    --DERRUBA INSCRIÇOES DOS ALUNOS PARA O ANO SEGUINTE
    UPDATE FI_AP SET
    	FI_AP.DT_ALTER_1          = GETDATE(),
    	FI_AP.HR_ALTER_1          = convert(int, replace(convert(varchar(10), GETDATE(), 108), ':', '')),
    	FI_AP.FL_SIT_INSCR        = 1,
    	FI_AP.CD_MOTIVO_CANCEL    = 99,
    	FI_AP.DT_INSCR_CANCEL     = GETDATE(),
    	FI_AP.LOGIN_EXCL          = '[AUTO COMPAT]',
    	FI_AP.MACHINE_EXCL        = '[AUTO COMPAT]',
    	FI_AP.USER_EXCL           = '[AUTO COMPAT]'
    FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL I (NOLOCK)
    INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI_AP(NOLOCK) ON FI_AP.ANO_LETIVO = @DT_ANO_LETIVO_ANO_SEGUINTE AND FI_AP.ID_ALUNO = I.CD_ALUNO AND ISNULL(FI_AP.FL_SIT_INSCR,0) = 0
    WHERE I.ID_RODADA = @ID_RODADA
    AND I.CD_MATRICULA_ALUNO > 0";

        #endregion

        #region CONVERSAO DO ABANDONO
        /// <summary>
        /// QUERY que faz a conversão do abandono
        /// verifica se a matricula anterior a criada era abandono, se for converte
        /// </summary>
        public static readonly string QueryConversaoAbandono = @"

        --PEGA OS DADOS DA MATRICULA NOVA
        DROP TABLE IF EXISTS #TMP_MA
        SELECT MA.CD_MATRICULA_ALUNO, MA.CD_ALUNO, MA.CD_TIPO_ENSINO_EQUIVALENTE, MA.NR_SERIE_EQUIVALENTE, T.CD_ESCOLA, T.CD_DURACAO, T.CD_TURMA
        INTO #TMP_MA
        FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL R (NOLOCK) 
        INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA (NOLOCK) ON MA.DT_ANO_LETIVO = @DT_ANO_LETIVO
														           AND MA.CD_MATRICULA_ALUNO = R.CD_MATRICULA_ALUNO
														           AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
        INNER JOIN DB_SARA.CADALUNOS.TB_TURMA T (NOLOCK) ON T.DT_ANO_LETIVO  = @DT_ANO_LETIVO
												        AND T.CD_TURMA = MA.CD_TURMA											
        WHERE R.ID_RODADA = @ID_RODADA

        --PEGA OS DADOS DA MATRICULA DE ABANDONO
        DROP TABLE IF EXISTS #TMP_MA_A
        SELECT MA.CD_MATRICULA_ALUNO, MA.CD_ALUNO, MA.CD_TIPO_ENSINO_EQUIVALENTE, MA.NR_SERIE_EQUIVALENTE, T.CD_ESCOLA, T.CD_DURACAO, T.CD_TURMA
        INTO #TMP_MA_A
        FROM #TMP_MA TMP (NOLOCK) 
        INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA (NOLOCK) ON MA.DT_ANO_LETIVO = @DT_ANO_LETIVO	
														           AND MA.CD_ALUNO = TMP.CD_ALUNO
														           AND MA.CD_MATRICULA_ALUNO < TMP.CD_MATRICULA_ALUNO
														           AND MA.FL_SITUACAO_ALUNO_CLASSE = 2
        INNER JOIN DB_SARA.CADALUNOS.TB_TURMA T (NOLOCK) ON T.DT_ANO_LETIVO  = @DT_ANO_LETIVO
												        AND T.CD_TURMA = MA.CD_TURMA			



        DROP TABLE IF EXISTS #TMP_CONVERSAO
        SELECT MA_ABAN.CD_MATRICULA_ALUNO, 
        CASE WHEN MA.CD_ESCOLA = MA_ABAN.CD_ESCOLA
		        AND
		        MA.CD_TURMA <> MA_ABAN.CD_TURMA
		        AND
		        MA.CD_DURACAO = MA_ABAN.CD_DURACAO THEN 17
	        ELSE 16
	        END TIPO
        INTO #TMP_CONVERSAO
        FROM #TMP_MA MA
        INNER JOIN #TMP_MA_A MA_ABAN
        ON MA_ABAN.CD_ALUNO = MA.CD_ALUNO
        AND MA_ABAN.CD_TIPO_ENSINO_EQUIVALENTE = MA.CD_TIPO_ENSINO_EQUIVALENTE
        AND MA_ABAN.NR_SERIE_EQUIVALENTE = MA.NR_SERIE_EQUIVALENTE
        AND (
            --	Escolas diferentes
            (
	            MA.CD_ESCOLA <> MA_ABAN.CD_ESCOLA
	            AND
	            (MA.CD_DURACAO = 0 OR (MA.CD_DURACAO = 1 AND MA_ABAN.CD_DURACAO IN (0, 1)) OR (MA.CD_DURACAO = 2 AND MA_ABAN.CD_DURACAO IN (0, 2)))
            )
            OR
            --	Mesma escola, mesma classe
            (
	            MA.CD_ESCOLA = MA_ABAN.CD_ESCOLA
	            AND
	            MA.CD_TURMA = MA_ABAN.CD_TURMA
	            AND
	            (MA.CD_DURACAO = 0 OR (MA.CD_DURACAO = 1 AND MA_ABAN.CD_DURACAO IN (0, 1)) OR (MA.CD_DURACAO = 2 AND MA_ABAN.CD_DURACAO IN (0, 2)))
            )
            OR
            --	Mesma escola, classes diferentes, mesmo período
            (
	            MA.CD_ESCOLA = MA_ABAN.CD_ESCOLA
	            AND
	            MA.CD_TURMA <> MA_ABAN.CD_TURMA
	            AND
	            MA.CD_DURACAO = MA_ABAN.CD_DURACAO
            )
            OR
            --	Mesma escola, classes diferentes, períodos diferentes
            (
	            MA.CD_ESCOLA = MA_ABAN.CD_ESCOLA
	            AND
	            MA.CD_DURACAO <> MA_ABAN.CD_DURACAO
	            AND
	            (MA.CD_DURACAO = 0 OR (MA.CD_DURACAO = 1 AND MA_ABAN.CD_DURACAO IN (0, 1)) OR (MA.CD_DURACAO = 2 AND MA_ABAN.CD_DURACAO IN (0, 2)))
            )
        )

        -- ATUALIZA MATRICULA ABANDONO
        UPDATE MA SET MA.FL_SITUACAO_ALUNO_CLASSE = TMP.TIPO
        FROM #TMP_CONVERSAO TMP
        INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA (NOLOCK) ON MA.DT_ANO_LETIVO = @DT_ANO_LETIVO AND MA.CD_MATRICULA_ALUNO = TMP.CD_MATRICULA_ALUNO AND MA.FL_SITUACAO_ALUNO_CLASSE = 2
        
        DROP TABLE IF EXISTS #TMP_MA
        DROP TABLE IF EXISTS #TMP_MA_A
        DROP TABLE IF EXISTS #TMP_CONVERSAO
        ";

        #endregion

        #region INATIVAR MATRICULA OUTRAS REDES
        /// <summary>
        /// Query Para Inativar matriculas ativas em outras redes
        /// Apenas para os tipos de ensino equivalente ao 2 e 14
        /// Valida a data de inicio do ano letivo para saber se coloca 99 ou 31(baixa Transferencia)
        /// </summary>
        public static string QueryInativarMatriculaOutrasRedes = @"
            DECLARE @DT_INICIO_ANO_LETIVO DATETIME2
    
			DROP TABLE IF EXISTS #TMP_ALUNOS_MATRICULADOS
            SELECT CD_MATRICULA_ALUNO, CD_ALUNO
            INTO #TMP_ALUNOS_MATRICULADOS
            FROM CALCULO_ROTAS.DBO.TB_REL_COMPAT_REAL (NOLOCK)
            WHERE ID_RODADA = @ID_RODADA
            AND ID_FASE IN (4,7)
            AND CD_MATRICULA_ALUNO > 0

            SET @DT_INICIO_ANO_LETIVO = (SELECT TOP 1 DT_INICIO_AULA 
                                                FROM CADALUNOS..TB_PARAMETRO (NOLOCK)
                                                WHERE ANO_LETIVO = @DT_ANO_LETIVO
                                                AND CD_TIPO_PARAMETRO = 2 -- ANO LETIVO
                                                AND COD_REDE_DE_ENSINO  = 1
                                                AND IC_STATUS = 1 ) -- REDE ESTADUAL
            
            IF(GETDATE() >= @DT_INICIO_ANO_LETIVO) -- Baixa por Transferencia
            BEGIN
				   UPDATE MA SET
    					MA.FL_SITUACAO_ALUNO_CLASSE = 31,
						MA.DT_FIM_MATRICULA			= @DT_FIM_MATRICULA,
    					MA.DT_ALTER_1		        = GETDATE(),
    					MA.LOGIN_ALTER			    = '[AUTO COMPAT]',
    					MA.MACHINE_ALTER            = '[AUTO COMPAT]',
    					MA.USER_ALTER               = '[AUTO COMPAT]'
					FROM #TMP_ALUNOS_MATRICULADOS TMP
					INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA (NOLOCK) ON MA.DT_ANO_LETIVO = @DT_ANO_LETIVO 
																			   AND MA.CD_ALUNO = TMP.CD_ALUNO 
																			   AND MA.FL_SITUACAO_ALUNO_CLASSE = 0 
																			   AND MA.CD_MATRICULA_ALUNO <> TMP.CD_MATRICULA_ALUNO
																			   AND MA.CD_TIPO_ENSINO_EQUIVALENTE IN (2, 14, 101)
					INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU (NOLOCK) ON TU.DT_ANO_LETIVO = @DT_ANO_LETIVO 
																	 AND TU.CD_TURMA = MA.CD_TURMA 
																	 AND TU.TP_REDE_ENSINO NOT IN(1,2)
            END
			ELSE -- Excluir
				BEGIN
					 UPDATE MA SET
    						MA.FL_SITUACAO_ALUNO_CLASSE = 99,
    						MA.DT_EXCL			        = GETDATE(),
    						MA.LOGIN_EXCL			    = '[AUTO COMPAT]',
    						MA.MACHINE_EXCL             = '[AUTO COMPAT]',
    						MA.USER_EXCL                = '[AUTO COMPAT]'
						FROM #TMP_ALUNOS_MATRICULADOS TMP
						INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA (NOLOCK) ON MA.DT_ANO_LETIVO = @DT_ANO_LETIVO 
																					AND MA.CD_ALUNO = TMP.CD_ALUNO 
																					AND MA.FL_SITUACAO_ALUNO_CLASSE = 0 
																					AND MA.CD_MATRICULA_ALUNO <> TMP.CD_MATRICULA_ALUNO
																					AND MA.CD_TIPO_ENSINO_EQUIVALENTE IN (2, 14, 101)
						INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU (NOLOCK) ON TU.DT_ANO_LETIVO = @DT_ANO_LETIVO 
																			AND TU.CD_TURMA = MA.CD_TURMA 
																		AND TU.TP_REDE_ENSINO NOT IN(1,2)
			END
                ";
        #endregion

        #region INATIVAR INTERESSE REMATRICULA
        /// <summary>
        /// Query Para Inativar Interesse de Rematricula
        /// </summary>
        public static string QueryInativarInteresseRematricula = @"
                --PEGA O QUE FOI COMPATIBILIZADO NA RODADA POR MOVIMENTAÇÃO
                ;WITH #W_COMPAT AS( SELECT C.CD_ALUNO,  C.CD_MATRICULA_ALUNO, C.ID_FICHA, I.CD_MATRICULA_ALUNO_ORIGEM
					                FROM CALCULO_ROTAS.DBO.TB_REL_COMPAT_REAL C (NOLOCK) 
					                INNER JOIN CADALUNOS.DBO.TB_FICHA_INSCRICAO I ON I.ANO_LETIVO         = @DT_ANO_LETIVO 
															                     AND I.ID_FICHA_INSCRICAO = C.ID_FICHA
					                WHERE C.ID_RODADA = @ID_RODADA
					                  AND C.ID_FASE IN (0,8,9)
					                  AND ISNULL(C.CD_MATRICULA_ALUNO, 0) > 0)

                -- PEGA O QUE COMPATIBILIZADO EM UMA ESCOLA QUE NAO SEJA DA REDE 1
                ,#W_MAT_DESTINO AS(SELECT TMP.* 
				                FROM #W_COMPAT TMP
				                INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA ON MA.DT_ANO_LETIVO = @DT_ANO_LETIVO 
															                      AND MA.CD_MATRICULA_ALUNO = TMP.CD_MATRICULA_ALUNO
				                INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU ON TU.DT_ANO_LETIVO = @DT_ANO_LETIVO 
													                    AND TU.CD_TURMA = MA.CD_TURMA 
														                AND TU.TP_REDE_ENSINO > 1)

                -- PEGA A ORIGEM DA INSCRIÇÃO E VERIFICA SE É DA REDE 1
                ,#W_MAT_ORIGEM AS(SELECT TMP.* 
					                FROM #W_MAT_DESTINO TMP
					                INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA ON MA.DT_ANO_LETIVO = @DT_ANO_LETIVO 
																                AND MA.CD_MATRICULA_ALUNO = TMP.CD_MATRICULA_ALUNO_ORIGEM
					                INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU ON TU.DT_ANO_LETIVO = @DT_ANO_LETIVO 
															                AND TU.CD_TURMA = MA.CD_TURMA 
															                AND TU.TP_REDE_ENSINO = 1)

                -- SE ALUNO POSSUI MATRICULA NA REDE 1 E FOI MOVIMENTADO PARA OUTRA REDE
                -- DERRUBA O INTERESSE DE REMATRICULA DO ALUNO
                UPDATE IR SET FL_ATIVO       = 0
                             ,FL_EXCLUIDO    = 1
			                 ,DATA_EXCLUSAO  = GETDATE()
			                 ,LOGIN_EXCLUSAO = 'COMPAT'
			                 ,USER_EXCLUSAO  = SYSTEM_USER
                FROM #W_MAT_ORIGEM TMP
                INNER JOIN DB_SARA.CADALUNOS.TB_INTERESSE_REMATRICULA IR ON IR.ANO_LETIVO_REMATRICULA  = @DT_ANO_LETIVO_INTERESSE 
														                AND IR.CODIGO_ALUNO            = TMP.CD_ALUNO
                ";
        #endregion

        #region EMAIL
        public static readonly string QueryAlunosMatriculados = @"
           ;WITH W_EMAILS AS (SELECT  A.NM_EMAIL AS EMAIL_ALUNO
		            ,(STUFF((SELECT ',' + REPLACE(ISNULL(R.NM_NM_EMAIL, ''), ',', ' ')
		            FROM CADALUNOS..TB_RESPONSAVEL_ALUNO RA (NOLOCK)
			            INNER JOIN CADALUNOS.DBO.TB_RESPONSAVEL R ON R.CD_RESPSV = RA.CD_RESPSV AND R.DT_EXCL IS NULL
		            WHERE RA.CD_ALUNO = RC.CD_ALUNO AND RA.DT_EXCL IS NULL
		            FOR XML PATH('')),1,1,'')) AS 'EMAIL_RESPOSAVEL'
		            ,RC.NM_ALUNO
		            ,RC.NM_COMPLETO_ESCOLA 
		            ,ISNULL((SELECT TOP 1 EMAIL_ADMIN_GEST
			            FROM DB_SCE.Escola.TB_CONTATO_UNIDADE CU 
			            INNER JOIN DB_SCE.Escola.TB_CONTATO C ON C.CD_CONTATO = CU.CD_CONTATO
		            WHERE CU.CD_ESCOLA = RC.CD_ESCOLA AND CU.CD_UNIDADE = RC.CD_UNIDADE) , '') AS EMAIL_ESCOLA
		            ,(SELECT TOP 1  ISNULL(DDD, '') + ' - ' +  ISNULL(NR_TELEFONE, '') AS TELEFONE_ESCOLA
			            FROM DB_SCE.Escola.TB_CONTATO_UNIDADE CU 
			            INNER JOIN  DB_SCE.Escola.TB_CONTATO C ON C.CD_CONTATO = CU.CD_CONTATO
		              WHERE CU.CD_ESCOLA = RC.CD_ESCOLA AND CU.CD_UNIDADE = RC.CD_UNIDADE AND IC_TELEFONE_PRINCIPAL = 1) AS TELEFONE_ESCOLA
                     ,A.NR_RA
					 ,ISNULL(A.NR_DIG_RA, '') AS  NR_DIG_RA
					 ,A.SG_UF_RA
            FROM CALCULO_ROTAS.DBO.TB_REL_COMPAT_REAL RC (NOLOCK)
            INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO A (NOLOCK) ON A.CD_ALUNO = RC.CD_ALUNO
            WHERE ID_RODADA = @ID_RODADA
            AND CD_MATRICULA_ALUNO > 0)

            SELECT* FROM W_EMAILS WHERE  EMAIL_ALUNO IS NOT NULL OR EMAIL_RESPOSAVEL IS NOT NULL;";
        #endregion

        #region TB_INTEGRACAO_SED_MUN
        public static readonly string QueryIntegracaoSedMun = @"

		
		;WITH W_ESCOLAS AS(SELECT ES.CD_ESCOLA
								 ,UN.CD_UNIDADE
				             FROM DB_SCE.Escola.TB_ESCOLA ES WITH(NOLOCK)
				            INNER JOIN DB_SCE.Escola.TB_UNIDADE UN WITH(NOLOCK) ON UN.CD_ESCOLA = ES.CD_ESCOLA AND UN.IC_UNIDADE_ATIVA = 1
                            INNER JOIN DB_SCE.Escola.TB_ENDERECO EN WITH(NOLOCK) ON EN.CD_ENDERECO = UN.CD_ENDERECO
							WHERE ES.CD_SITUACAO = 1 -- Apenas escolas ativas
							  AND ES.CD_REDE_ENSINO IN(1, 2)
							  AND EN.CD_MUNICIPIO = 100),

     W_FICHA AS(SELECT U.ID_FICHA_INSCRICAO, U.CD_ALUNO
                  FROM (SELECT FI.ID_FICHA_INSCRICAO
				              ,CAST(FI.ID_ALUNO AS INT) AS 'CD_ALUNO'
                          FROM W_ESCOLAS TMP
                         INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI WITH(NOLOCK) 
						    ON FI.CD_ESCOLA  = TMP.CD_ESCOLA -- Alunos que vieram de escolas do município de SP
						   AND FI.CD_UNIDADE = TMP.CD_UNIDADE -- NOVO UNIDADE
                           AND FI.FL_FASE IN (0, 1, 4, 8, 9) -- Deslocamento
						 INNER JOIN CALCULO_ROTAS.dbo.TB_REL_COMPAT_REAL RCR WITH(NOLOCK) 
						    ON RCR.ID_FICHA = FI.ID_FICHA_INSCRICAO
						 WHERE RCR.ID_RODADA = @ID_RODADA
                         UNION -- Ficou infinitamente mais rápido usar o UNION do que um OR!
                        SELECT FI.ID_FICHA_INSCRICAO
						      ,CAST(FI.ID_ALUNO AS INT) AS 'CD_ALUNO'
                          FROM W_ESCOLAS TMP
                         INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI WITH(NOLOCK) 
						    ON FI.CD_ESCOL_COMP_DEF = TMP.CD_ESCOLA -- Alunos que foram alocados em escolas do município de SP, independente de onde vieram
                           AND FI.FL_FASE IN (0, 1, 4, 8, 9)
						 INNER JOIN CALCULO_ROTAS.dbo.TB_REL_COMPAT_REAL RCR WITH(NOLOCK) 
						    ON RCR.ID_FICHA = FI.ID_FICHA_INSCRICAO
						 WHERE RCR.ID_RODADA = @ID_RODADA) AS U),

		W_FICHA_COMPAT AS(SELECT FI.ID_FICHA_INSCRICAO
		                        ,FI.ID_GRAU
								,FI.ID_SERIE
								,FI.FL_FASE
								,FI.CD_ESCOL_COMP_DEF
								,FI.CD_UNID_COMP_DEF
								,FI.ID_DEFIC
								,FI.DT_COMP_DEF
								,FI.ANO_LETIVO
								,WF.CD_ALUNO
							    ,FI.DT_COMP_DEF AS 'DT_OPERACAO'
							    ,FI.DT_COMP_DEF AS 'DT_INCLUSAO'
							FROM W_FICHA WF
						   INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI WITH(NOLOCK) 
							  ON FI.ID_FICHA_INSCRICAO   = WF.ID_FICHA_INSCRICAO)


 INSERT INTO CADALUNOS.DBO.TB_INTEGRACAO_SED_MUN(FL_PROC				
												,ID_FICHA_INSCRICAO     
												,ANO_LETIVO     		
												,COD_ESC             	
												,COD_UNID            	
												,FL_FASE           		
												,ID_GRAU          		
												,ID_SERIE         		
												,IND_NECES_ESPEC       	
												,CD_ALUNO				
												,NM_ALUNO          		
												,NM_FILIAL_1			
												,NM_FILIAL_2			
												,NM_FILIAL_3			
												,TP_FILIAL_1			
												,TP_FILIAL_2			
												,TP_FILIAL_3			
												,DT_NASCIMENTO         	
												,TP_SEXO          		
												,NR_RG              	
												,NR_DIG_RG          	
												,SIGLA_UF_RG         	
												,END_CEP             	
												,END_UF              	
												,END_NUM             	
												,END_BAIRRO            	
												,END_MUN               	
												,END_LOGR              	
												,END_COMPL            	
												,DT_INCLUSAO
												,ID_RODADA)
                            		     (SELECT 1 AS 'FL_PROC'
												,FC.ID_FICHA_INSCRICAO     
												,FC.ANO_LETIVO     		
												,FC.CD_ESCOL_COMP_DEF AS 'COD_ESC'             	
												,FC.CD_UNID_COMP_DEF  AS 'COD_UNID'
												,FC.FL_FASE           		
												,FC.ID_GRAU          		
												,FC.ID_SERIE         		
												,FC.ID_DEFIC AS 'IND_NECES_ESPEC'       	
												,FC.CD_ALUNO				
												,AL.NM_ALUNO          		
												,AL.NM_FILIAL_1			
												,AL.NM_FILIAL_2			
												,AL.NM_FILIAL_3			
												,AL.TP_FILIAL_1			
												,AL.TP_FILIAL_2			
												,AL.TP_FILIAL_3			
												,AL.DT_NASCMTO AS 'DT_NASCIMENTO'
												,CASE WHEN AL.TP_SEXO = 'M' THEN 1
												      WHEN AL.TP_SEXO = 'F' THEN 2
                            					      WHEN AL.TP_SEXO NOT IN('M','F','1','2') THEN 0 ELSE AL.TP_SEXO
												  END									 AS 'TP_SEXO'
												,AL.NR_RA								 AS 'NR_RG'
												,AL.NR_DIG_RA							 AS 'NR_DIG_RG'
												,AL.SG_UF_RA							 AS 'SIGLA_UF_RG'
												,CONVERT(VARCHAR(8),AL.NR_CEP)			 AS 'END_CEP'
												,CONVERT(CHAR(2),AL.SG_UF)			     AS 'END_UF'
												,CONVERT(VARCHAR(10),AL.EN_NR_EN)		 AS 'END_NUM'
												,CONVERT(VARCHAR(30),AL.NM_BAIRRO)		 AS 'END_BAIRRO'
												,CONVERT(VARCHAR(22),AL.NM_CIDADE)		 AS 'END_MUN'
												,CONVERT(VARCHAR(40),AL.EN_RUA)			 AS 'END_LOGR'
												,CONVERT(VARCHAR(13),AL.EN_COMPLEMTO_EN) AS 'END_COMPL'
												,FC.DT_INCLUSAO
												,@ID_RODADA								 AS 'ID_RODADA'
										  FROM W_FICHA_COMPAT FC
										 INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO AL WITH(NOLOCK)
                            				ON AL.CD_ALUNO = FC.CD_ALUNO
										 WHERE ISNULL(FC.CD_ESCOL_COMP_DEF,0) > 0)
";
        #endregion

        #region LOG VAGAS POR ESCOLA
        public static readonly string QueryLogVagas = @"

		DROP TABLE IF EXISTS #TMP_UNIDADES_VAGAS

        SELECT ES.CD_ESCOLA, UN.CD_UNIDADE, ES.CD_DIRETORIA_ESTADUAL, ES.CD_REDE_ENSINO, MUN.CD_DNE, 
               EN.DS_LATITUDE, EN.DS_LONGITUDE,
               IIF(EXISTS (SELECT TOP 1 1 FROM DB_SCE.Escola.TB_DEPENDENCIA_UNIDADE DU (NOLOCK) 
                            WHERE DU.CD_ESCOLA = ES.CD_ESCOLA 
                                AND DU.CD_UNIDADE = UN.CD_UNIDADE 
                                AND DU.CD_TP_DEPENDENCIA IN (50, 100)), 1, 0) ACESSIVEL,
        ES.NM_COMPLETO_ESCOLA, DI.NM_DIRETORIA, MUN.NM_MUNICIPIO
        INTO #TMP_UNIDADES_VAGAS
        FROM DB_SCE.Escola.TB_ESCOLA ES WITH(NOLOCK)
        INNER JOIN DB_SCE.Escola.TB_UNIDADE UN WITH(NOLOCK) ON UN.CD_ESCOLA = ES.CD_ESCOLA 
                                                            AND UN.IC_UNIDADE_ATIVA = 1
        INNER JOIN DB_SCE.Escola.TB_ENDERECO EN WITH(NOLOCK) ON EN.CD_ENDERECO = UN.CD_ENDERECO
        INNER JOIN DB_SCE.Escola.TB_MUNICIPIO MUN WITH(NOLOCK) ON MUN.CD_MUNICIPIO = EN.CD_MUNICIPIO
        INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO CM WITH(NOLOCK) ON CM.DT_ANO_LETIVO = @DT_ANO_LETIVO
                                                                                AND CM.CD_DNE = MUN.CD_DNE 
                                                                                AND CM.FL_ATIVO = 1
        INNER JOIN CADALUNOS.DBO.TB_COMPATIBILIZACAO_MUNICIPIO_REDE CMR WITH(NOLOCK) ON CMR.CD_COMPATIBILIZACAO_MUNICIPIO = CM.CD_COMPATIBILIZACAO_MUNICIPIO 
                                                                                    AND CMR.FL_ATIVO = 1 
																			        AND CMR.CD_REDE_ENSINO = ES.CD_REDE_ENSINO
        INNER JOIN DB_SCE.Escola.TB_DIRETORIA DI WITH(NOLOCK) ON DI.CD_DIRETORIA = ES.CD_DIRETORIA_ESTADUAL
        WHERE (ES.CD_SITUACAO = 1 OR ES.CD_ESCOLA = 573334) -- Apenas escolas ativas
        AND ISNULL(ES.CD_TP_IDENTIFICADOR, 0) NOT IN(10) -- não irá compatibilizar escolas indigena

        --Inclui escolas de excecao - Nova regra Liceu
        INSERT INTO #TMP_UNIDADES_VAGAS (CD_ESCOLA, CD_UNIDADE, CD_DIRETORIA_ESTADUAL, CD_REDE_ENSINO, CD_DNE, DS_LATITUDE, DS_LONGITUDE, ACESSIVEL, NM_COMPLETO_ESCOLA, NM_DIRETORIA, NM_MUNICIPIO)
		SELECT ES.CD_ESCOLA,
			   UN.CD_UNIDADE,
			   ES.CD_DIRETORIA_ESTADUAL,
			   EC.CD_REDE_ENSINO,
			   MU.CD_DNE,
			   EN.DS_LATITUDE,
			   EN.DS_LONGITUDE,
			   IIF(EXISTS (SELECT TOP 1 1 
                        FROM DB_SCE.Escola.TB_DEPENDENCIA_UNIDADE DU (NOLOCK) 
                        WHERE DU.CD_ESCOLA = ES.CD_ESCOLA 
                          AND DU.CD_UNIDADE = UN.CD_UNIDADE 
                          AND DU.CD_TP_DEPENDENCIA IN (50, 100)), 1, 0) ACESSIVEL,
						  ES.NM_COMPLETO_ESCOLA
         ,DI.NM_DIRETORIA
         ,MU.NM_MUNICIPIO
		FROM DB_SARA.dbo.TB_EXCECAO_COMPAT EC (NOLOCK)
			INNER JOIN DB_SCE.Escola.TB_ESCOLA ES WITH (NOLOCK)
				ON ES.CD_SITUACAO = 1
				   AND EC.CD_ESCOLA = ES.CD_ESCOLA
			INNER JOIN DB_SCE.Escola.TB_UNIDADE UN WITH (NOLOCK)
				ON UN.CD_ESCOLA = ES.CD_ESCOLA
				   AND UN.IC_UNIDADE_ATIVA = 1
			INNER JOIN DB_SCE.Escola.TB_ENDERECO EN WITH (NOLOCK)
				ON EN.CD_ENDERECO = UN.CD_ENDERECO
			INNER JOIN DB_SCE.Escola.TB_MUNICIPIO MU WITH (NOLOCK)
				ON MU.CD_MUNICIPIO = EN.CD_MUNICIPIO
				   AND MU.CD_MUNICIPIO = EN.CD_MUNICIPIO
				   AND EN.DS_LATITUDE IS NOT NULL
				   AND EN.DS_LONGITUDE IS NOT NULL
				   INNER JOIN DB_SCE.Escola.TB_DIRETORIA DI WITH(NOLOCK) ON DI.CD_DIRETORIA = ES.CD_DIRETORIA_ESTADUAL
		WHERE EC.DT_ANO_LETIVO = @DT_ANO_LETIVO
		AND GETDATE() BETWEEN EC.DT_INI_VIG AND EC.DT_FIM_VIG


        INSERT INTO CADALUNOS.DBO.TB_REL_COMPAT_LOG_VAGAS (ANO_LETIVO, CD_ESCOLA, CD_UNIDADE, CD_TIPO_ENSINO, NR_SERIE, QTDE_VAGAS_DISPONIVEIS, ID_RODADA)
        SELECT TU.DT_ANO_LETIVO
              ,TU.CD_ESCOLA
	          ,TU.CD_UNIDADE
	          ,TU.CD_TIPO_ENSINO
	          ,TU.NR_SERIE
	          ,SUM(TU.QTDE_VAGAS_DISPONIVEIS) AS QTDE_VAGAS_DISPONIVEIS
	          ,@ID_RODADA AS ID_RODADA
        FROM #TMP_UNIDADES_VAGAS TMP
        INNER JOIN DB_SARA.CADALUNOS.TB_TURMA TU WITH(NOLOCK) ON TU.DT_ANO_LETIVO = @DT_ANO_LETIVO
                                                        AND TU.CD_ESCOLA = TMP.CD_ESCOLA
                                                        AND TU.CD_UNIDADE = TMP.CD_UNIDADE
                                                        AND TU.CD_SITUACAO = 0 -- ATIVAS
                                                        AND TU.NR_SERIE <> 0 -- NÃO CONSIDERAR MULTISSERIADAS
                                                        AND ISNULL(TU.CD_DURACAO, 0) <> 2 -- ANUAL OU 1O SEMESTRE
                                                        --AND ISNULL(TU.CD_TIPO_CLASSE, 0) NOT IN (1, 2, 5, 10, 18, 20) -- NÃO CONSIDERAR CLASSES ESPECIAIS/MULTISSERIADAS
                                                        AND ISNULL(TU.CD_TIPO_CLASSE, 0) IN (0, 17, 18, 32) -- CLASSES REGULARES, INTEGRAIS E VENCE
                                                        AND TU.DT_FIM_AULA >= GETDATE() -- NÃO CONSIDERAR CLASSES ENCERRADAS
        GROUP BY TU.DT_ANO_LETIVO, TU.CD_ESCOLA, TU.CD_UNIDADE, TU.CD_TIPO_ENSINO, TU.NR_SERIE

        DROP TABLE IF EXISTS #TMP_UNIDADES_VAGAS
";
        #endregion

    }
}
