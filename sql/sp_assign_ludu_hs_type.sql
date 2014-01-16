USE [concep_sr12]
GO

/****** Object:  StoredProcedure [dbo].[sp_assign_ludu_hs_type]    Script Date: 07/22/2010 10:34:20 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'sp_assign_ludu_hs_type', 'p') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_assign_ludu_hs_type];

CREATE PROCEDURE [dbo].[sp_assign_ludu_hs_type]
@schema nvarchar(max) = N'dbo',
--schema is "dbo"
@table_name nvarchar(max) = N'hs_points_from_landcore',
--table name is the from table or "hs_points_from_landcore"
@estimates_year int
AS
    BEGIN
	SET NOCOUNT ON;
	SET @table_name = QUOTENAME(LTRIM(RTRIM(@table_name)));
	DECLARE @cmd AS NVARCHAR(max);
	--First zero out du fields for this ludu_year
	SET @cmd = N'UPDATE ' + QUOTENAME(@schema) + N'.' + @table_name +
		N' SET hs_sf = 0, hs_sfmu = 0, hs_mf = 0, hs_mh = 0 WHERE ludu_year = @ludu_year';
	EXEC sp_executesql @cmd, N'@ludu_year int', @estimates_year;
	--Then update them according to assignment algorithm
	SET @cmd = N'UPDATE ' + QUOTENAME(@schema) + N'.' + @table_name +
		N' SET hs_sf = (SELECT
			CASE
				WHEN (lu IN (1000, 1110, 1190)) OR (lu BETWEEN 8000 AND 8003) THEN du
				ELSE
					CASE
						WHEN lu > 1300 THEN
							CASE
								WHEN du = 1 THEN du
								ELSE 0
							END
						ELSE 0
					END
			END),
		hs_sfmu = (SELECT
			CASE
				WHEN lu IN (1120) THEN du
				else 0			
			END),
		hs_mf = (SELECT
			CASE
				WHEN lu BETWEEN 1200 AND 1290 THEN du
				ELSE
					CASE
						WHEN lu > 1300 AND (lu NOT BETWEEN 8000 AND 8003) and du > 1 THEN du
						ELSE 0
					END
			END),
		hs_mh = (SELECT
			CASE
				WHEN lu = 1300 THEN du
				ELSE 0
			END)
		WHERE ludu_year = @ludu_year';
	EXEC sp_executesql @cmd, N'@ludu_year int', @estimates_year;
END

GO


