select ludu_year,sum(hs)as hs, sum(hs_sf) as sf, sum(hs_sfmu) as sfmu, sum(hs_mf) as mf, sum(hs_mh) as mh
from concep_for_2005.dbo.hs_from_landcore group by ludu_year order by ludu_year