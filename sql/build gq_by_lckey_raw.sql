truncate table concep_for_2005.dbo.gq_by_lckey_raw;
insert into concep_for_2005.dbo.gq_by_lckey_raw
select * from concep_for_2005.dbo.gq2005_lckey2005;
insert into concep_for_2005.dbo.gq_by_lckey_raw
select * from concep_for_2005.dbo.gq2006_lckey2006;
insert into concep_for_2005.dbo.gq_by_lckey_raw
select * from concep_for_2005.dbo.gq2007_lckey2007;
insert into concep_for_2005.dbo.gq_by_lckey_raw
select * from concep_for_2005.dbo.gq2008_lckey2008;