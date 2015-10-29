

update hs_from_landcore set hs_sf = hs where lu in (1000,1100,1110,8000,8001,8002,8003,7600,7603,7605,7609);
update hs_from_landcore set hs_sfmu = hs where lu = 1120;
update hs_from_landcore set hs_mh = hs where lu = 1300;
update hs_from_landcore set hs_mf = hs where hs_sf+hs_sfmu+hs_mh = 0;


