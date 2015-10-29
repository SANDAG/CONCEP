select * from concep_for_2005.dbo.income_estimates_mgra where hh <> i1+i2+i3+i4+i5+i6+i7+i8+i9+i10 and estimates_year = 2005;
select * from concep_for_2005.dbo.income_estimates_mgra where i1 < 0 or i2 < 0 or i3 < 0 or i4 < 0 or i5 < 0 and estimates_year = 2005;
select * from concep_for_2005.dbo.income_estimates_mgra where i6 < 0 or i7 < 0 or i8 < 0 or i9 < 0 or i10 < 0 and estimates_year = 2005;
select p.hh, i.hh from concep_for_2005.dbo.income_estimates_mgra i, concep_for_2005.dbo.popest_mgra p
      where p.estimates_year = i.estimates_year and p.mgra = i.mgra and p.hh <> i.hh and p.estimates_year = 2005;
      
select * from concep_for_2005.dbo.income_estimates_mgra where hh <> i1+i2+i3+i4+i5+i6+i7+i8+i9+i10 and estimates_year = 2006;
select * from concep_for_2005.dbo.income_estimates_mgra where i1 < 0 or i2 < 0 or i3 < 0 or i4 < 0 or i5 < 0 and estimates_year = 2006;
select * from concep_for_2005.dbo.income_estimates_mgra where i6 < 0 or i7 < 0 or i8 < 0 or i9 < 0 or i10 < 0 and estimates_year = 2006;
select p.hh, i.hh from concep_for_2005.dbo.income_estimates_mgra i, concep_for_2005.dbo.popest_mgra p
      where p.estimates_year = i.estimates_year and p.mgra = i.mgra and p.hh <> i.hh and p.estimates_year = 2006;
      
select * from concep_for_2005.dbo.income_estimates_mgra where hh <> i1+i2+i3+i4+i5+i6+i7+i8+i9+i10 and estimates_year = 2007;
select * from concep_for_2005.dbo.income_estimates_mgra where i1 < 0 or i2 < 0 or i3 < 0 or i4 < 0 or i5 < 0 and estimates_year = 2007;
select * from concep_for_2005.dbo.income_estimates_mgra where i6 < 0 or i7 < 0 or i8 < 0 or i9 < 0 or i10 < 0 and estimates_year = 2007;
select p.hh, i.hh from concep_for_2005.dbo.income_estimates_mgra i, concep_for_2005.dbo.popest_mgra p
      where p.estimates_year = i.estimates_year and p.mgra = i.mgra and p.hh <> i.hh and p.estimates_year = 2007;
      
select * from concep_for_2005.dbo.income_estimates_mgra where hh <> i1+i2+i3+i4+i5+i6+i7+i8+i9+i10 and estimates_year = 2008;
select * from concep_for_2005.dbo.income_estimates_mgra where i1 < 0 or i2 < 0 or i3 < 0 or i4 < 0 or i5 < 0 and estimates_year = 2008;
select * from concep_for_2005.dbo.income_estimates_mgra where i6 < 0 or i7 < 0 or i8 < 0 or i9 < 0 or i10 < 0 and estimates_year = 2008;
select p.hh, i.hh from concep_for_2005.dbo.income_estimates_mgra i, concep_for_2005.dbo.popest_mgra p
      where p.estimates_year = i.estimates_year and p.mgra = i.mgra and p.hh <> i.hh and p.estimates_year = 2008;
      
select * from concep_for_2005.dbo.income_estimates_mgra where hh <> i1+i2+i3+i4+i5+i6+i7+i8+i9+i10 and estimates_year = 2009;
select * from concep_for_2005.dbo.income_estimates_mgra where i1 < 0 or i2 < 0 or i3 < 0 or i4 < 0 or i5 < 0 and estimates_year = 2009;
select * from concep_for_2005.dbo.income_estimates_mgra where i6 < 0 or i7 < 0 or i8 < 0 or i9 < 0 or i10 < 0 and estimates_year = 2009;
select p.hh, i.hh from concep_for_2005.dbo.income_estimates_mgra i, concep_for_2005.dbo.popest_mgra p
      where p.estimates_year = i.estimates_year and p.mgra = i.mgra and p.hh <> i.hh and p.estimates_year = 2009;