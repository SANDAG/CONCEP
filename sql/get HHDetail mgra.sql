/****** Script for SelectTopNRows command from SSMS  ******/
SELECT estimates_year
      ,sum(hhs1) as hhs1
      ,sum(hhs2) as hhs2
      ,sum(hhs3) as hhs3
      ,sum(hhs4) as hhs4
      ,sum(hhs5) as hhs5
      ,sum(hhs6) as hhs6
      ,sum(hhs7) as hhs7
      ,sum(hhwoc) as hhwoc
      ,sum(hhwc) as hhwc
      ,sum(hhworkers0) as hhworkers0
      ,sum(hhworkers1) as hhworkers1
      ,sum(hhworkers2) as hhworkers2
      ,sum(hhworkers3) as hhworkers3
      ,sum(hh) as hh
  FROM [concep_for_2005].[dbo].[popest_HHdetail_mgra] group by estimates_year order by estimates_year