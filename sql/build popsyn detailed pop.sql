truncate table concep.dbo.xxx;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,1 as age,popm_0to4 from concep.dbo.detailed_pop_tab_mgra where popm_0to4 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,1 as age,popf_0to4 from concep.dbo.detailed_pop_tab_mgra where popf_0to4 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,2 as age,popm_5to9 from concep.dbo.detailed_pop_tab_mgra where popm_5to9 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,2 as age,popf_5to9 from concep.dbo.detailed_pop_tab_mgra where popf_5to9 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,3 as age,popm_10to14 from concep.dbo.detailed_pop_tab_mgra where popm_10to14 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,3 as age,popf_10to14 from concep.dbo.detailed_pop_tab_mgra where popf_10to14 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,4 as age,popm_15to17 from concep.dbo.detailed_pop_tab_mgra where popm_15to17 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,4 as age,popf_15to17 from concep.dbo.detailed_pop_tab_mgra where popf_15to17 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,5 as age,popm_18to19 from concep.dbo.detailed_pop_tab_mgra where popm_18to19 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,5 as age,popf_18to19 from concep.dbo.detailed_pop_tab_mgra where popf_18to19 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,6 as age,popm_20to24 from concep.dbo.detailed_pop_tab_mgra where popm_20to24 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,6 as age,popf_20to24 from concep.dbo.detailed_pop_tab_mgra where popf_20to24 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,7 as age,popm_25to29 from concep.dbo.detailed_pop_tab_mgra where popm_25to29 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,7 as age,popf_25to29 from concep.dbo.detailed_pop_tab_mgra where popf_25to29 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,8 as age,popm_30to34 from concep.dbo.detailed_pop_tab_mgra where popm_30to34 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,8 as age,popf_30to34 from concep.dbo.detailed_pop_tab_mgra where popf_30to34 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,9 as age,popm_35to39 from concep.dbo.detailed_pop_tab_mgra where popm_35to39 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,9 as age,popf_35to39 from concep.dbo.detailed_pop_tab_mgra where popf_35to39 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,10 as age,popm_40to44 from concep.dbo.detailed_pop_tab_mgra where popm_40to44 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,10 as age,popf_40to44 from concep.dbo.detailed_pop_tab_mgra where popf_40to44 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,11 as age,popm_45to49 from concep.dbo.detailed_pop_tab_mgra where popm_45to49 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,11 as age,popf_45to49 from concep.dbo.detailed_pop_tab_mgra where popf_45to49 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,12 as age,popm_50to54 from concep.dbo.detailed_pop_tab_mgra where popm_50to54 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,12 as age,popf_50to54 from concep.dbo.detailed_pop_tab_mgra where popf_50to54 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,13 as age,popm_55to59 from concep.dbo.detailed_pop_tab_mgra where popm_55to59 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,13 as age,popf_55to59 from concep.dbo.detailed_pop_tab_mgra where popf_55to59 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,14 as age,popm_60to61 from concep.dbo.detailed_pop_tab_mgra where popm_60to61 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,14 as age,popf_60to61 from concep.dbo.detailed_pop_tab_mgra where popf_60to61 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,15 as age,popm_62to64 from concep.dbo.detailed_pop_tab_mgra where popm_62to64 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,15 as age,popf_62to64 from concep.dbo.detailed_pop_tab_mgra where popf_62to64 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,16 as age,popm_65to69 from concep.dbo.detailed_pop_tab_mgra where popm_65to69 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,16 as age,popf_65to69 from concep.dbo.detailed_pop_tab_mgra where popf_65to69 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,17 as age,popm_70to74 from concep.dbo.detailed_pop_tab_mgra where popm_70to74 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,17 as age,popf_70to74 from concep.dbo.detailed_pop_tab_mgra where popf_70to74 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,18 as age,popm_75to79 from concep.dbo.detailed_pop_tab_mgra where popm_75to79 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,18 as age,popf_75to79 from concep.dbo.detailed_pop_tab_mgra where popf_75to79 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,19 as age,popm_80to84 from concep.dbo.detailed_pop_tab_mgra where popm_80to84 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,19 as age,popf_80to84 from concep.dbo.detailed_pop_tab_mgra where popf_80to84 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,20 as age,popm_85plus from concep.dbo.detailed_pop_tab_mgra where popm_85plus > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,20 as age,popf_85plus from concep.dbo.detailed_pop_tab_mgra where popf_85plus > 0;
delete from concep.dbo.xxx where estimates_year = 2011;

insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,1 as age,popm_0to4 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_0to4 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,1 as age,popf_0to4 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_0to4 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,2 as age,popm_5to9 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_5to9 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,2 as age,popf_5to9 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_5to9 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,3 as age,popm_10to14 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_10to14 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,3 as age,popf_10to14 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_10to14 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,4 as age,popm_15to17 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_15to17 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,4 as age,popf_15to17 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_15to17 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,5 as age,popm_18to19 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_18to19 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,5 as age,popf_18to19 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_18to19 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,6 as age,popm_20to24 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_20to24 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,6 as age,popf_20to24 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_20to24 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,7 as age,popm_25to29 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_25to29 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,7 as age,popf_25to29 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_25to29 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,8 as age,popm_30to34 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_30to34 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,8 as age,popf_30to34 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_30to34 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,9 as age,popm_35to39 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_35to39 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,9 as age,popf_35to39 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_35to39 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,10 as age,popm_40to44 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_40to44 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,10 as age,popf_40to44 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_40to44 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,11 as age,popm_45to49 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_45to49 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,11 as age,popf_45to49 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_45to49 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,12 as age,popm_50to54 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_50to54 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,12 as age,popf_50to54 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_50to54 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,13 as age,popm_55to59 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_55to59 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,13 as age,popf_55to59 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_55to59 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,14 as age,popm_60to61 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_60to61 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,14 as age,popf_60to61 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_60to61 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,15 as age,popm_62to64 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_62to64 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,15 as age,popf_62to64 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_62to64 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,16 as age,popm_65to69 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_65to69 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,16 as age,popf_65to69 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_65to69 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,17 as age,popm_70to74 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_70to74 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,17 as age,popf_70to74 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_70to74 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,18 as age,popm_75to79 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_75to79 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,18 as age,popf_75to79 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_75to79 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,19 as age,popm_80to84 from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_80to84 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,19 as age,popf_80to84 from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_80to84 > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,1 as sex,20 as age,popm_85plus from concep_for_2005.dbo.detailed_pop_tab_mgra where popm_85plus > 0;
insert into concep.dbo.xxx
select estimates_year,mgra,ethnicity,2 as sex,20 as age,popf_85plus from concep_for_2005.dbo.detailed_pop_tab_mgra where popf_85plus > 0;
delete from concep.dbo.xxx where estimates_year in(2004,2006,2007)
