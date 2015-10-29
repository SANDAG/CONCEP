drop table concep_for_2005.dbo.popsyn_2005;
select p.estimates_year,p.mgra,pop,hhp,gq,gq_civ,gq_civ_college,gq_civ_other,gq_mil,hs,hs_sf+hs_sfmu as hs_sf, hs_mf, hs_mh,
       p.hh, hh_sf+hh_sfmu as hh_sf, hh_mf, hh_mh, i1,i2,i3,i4,i5,i6,i7,i8,i9,i10,hhs1,hhs2,hhs3,hhs4,hhs5,hhs6,hhs7,1 as hhs,
       hhwoc, hhwc, hhworkers0, hhworkers1, hhworkers2, hhworkers3 into concep_for_2005.dbo.popsyn_2005
       from concep_for_2005.dbo.popest_mgra p, concep_for_2005.dbo.income_estimates_mgra i, concep_for_2005.dbo.popest_HHDetail_mgra d
       where p.estimates_year = 2005 and p.estimates_year = d.estimates_year and p.estimates_year = i.estimates_year
       and p.mgra = i.mgra and p.mgra = d.mgra ;
       update concep_for_2005.dbo.popsyn_2005 set hhs  = cast(hhp as float)/cast (hh as float) where hh > 0;
       
       drop table concep_for_2005.dbo.popsyn_2008;
select p.estimates_year,p.mgra,pop,hhp,gq,gq_civ,gq_civ_college,gq_civ_other,gq_mil,hs,hs_sf+hs_sfmu as hs_sf, hs_mf, hs_mh,
       p.hh, hh_sf+hh_sfmu as hh_sf, hh_mf, hh_mh, i1,i2,i3,i4,i5,i6,i7,i8,i9,i10,hhs1,hhs2,hhs3,hhs4,hhs5,hhs6,hhs7,1 as hhs,
       hhwoc, hhwc, hhworkers0, hhworkers1, hhworkers2, hhworkers3 into concep_for_2005.dbo.popsyn_2008
       from concep_for_2005.dbo.popest_mgra p, concep_for_2005.dbo.income_estimates_mgra i, concep_for_2005.dbo.popest_HHDetail_mgra d
       where p.estimates_year = 2008 and p.estimates_year = d.estimates_year and p.estimates_year = i.estimates_year
       and p.mgra = i.mgra and p.mgra = d.mgra ;
       update concep_for_2005.dbo.popsyn_2008 set hhs  = cast(hhp as float)/cast (hh as float) where hh > 0;