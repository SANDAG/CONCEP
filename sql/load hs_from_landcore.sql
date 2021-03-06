/****** Script for SelectTopNRows command from SSMS  ******/
truncate table concep_for_2005.dbo.hs_from_landcore;
insert into concep_for_2005.dbo.hs_from_landcore
SELECT 2005,[lcKey],mgra13
      ,[lu]
      ,[du],0,0,0,0
     
  FROM [concep_for_2005].[dbo].[ludu2005mgra13] where du > 0;
  
insert into concep_for_2005.dbo.hs_from_landcore
SELECT 2006,[lcKey],mgra13
      ,[lu]
      ,[du],0,0,0,0
     
  FROM [concep_for_2005].[dbo].[ludu2006mgra13] where du > 0;
  
insert into concep_for_2005.dbo.hs_from_landcore 
SELECT 2007,[lcKey],mgra13
      ,[lu]
      ,[du],0,0,0,0
     
  FROM [concep_for_2005].[dbo].[ludu2007mgra13] where du > 0;
  
insert into concep_for_2005.dbo.hs_from_landcore 
SELECT 2008,[lcKey],mgra13
      ,[lu]
      ,[du],0,0,0,0
     
  FROM [concep_for_2005].[dbo].[ludu2008mgra13] where du > 0;
  
insert into concep_for_2005.dbo.hs_from_landcore   
  SELECT 2009,[lcKey],mgra13
      ,[lu]
      ,[du],0,0,0,0
     
  FROM [concep_for_2005].[dbo].[ludu2009mgra13] where du > 0;