/****** Script for SelectTopNRows command from SSMS  ******/
SELECT x.city
      ,sum(pop)
      ,sum(hhp)
      ,sum(gq)
      ,sum(gq_civ)
      ,sum(gq_mil)
      ,sum(gq_civ_college)
      ,sum(gq_civ_other)
      ,sum(hs)
      ,sum(hs_sf)
      ,sum(hs_sfmu)
      ,sum(hs_mf)
      ,sum(hs_mh)
      ,sum(hh)
      ,sum(hh_sf)
      ,sum(hh_sfmu)
      ,sum(hh_mf)
      ,sum(hh_mh)
     
  FROM concep_for_2005.dbo.popest_mgra p, concep_for_2005.dbo.xref_mgra_sr13 x where x.mgra = p.mgra and estimates_year = 2006 group by x.city order by city
  