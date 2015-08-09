/*  NEW TABLES
  Scripts creation solaSW !
 
drop table inverter
;
drop table site
;
CREATE TABLE dbo.site(
	-- company
	company varchar(50) not NULL,

	-- site specific information
	name varchar(50) NOT NULL primary key,
	longitude decimal(9, 6) default 0,
	latitude decimal(9, 6) default 0,
	city varchar(50) not null,
	country varchar(50) not null,
    sitePower bigint not null
 )
;
CREATE TABLE dbo.inverter(
	siteName varchar(50) NOT NULL references site(name),
	
	-- Inverter specific information
	serialNo varchar(50) NOT NULL primary key,
	otherName varchar(50) not null,
	inverterPower bigint not null,
	model varchar(50) not null,
	type tinyint not NULL,
	sensorSN varchar(50) null
)
;
drop table mDc_northHillFarm
;
CREATE TABLE dbo.mDc_northHillFarm(
	dateMeasure dateTime NOT NULL,
	invString varchar(50) NULL,
	currentAmp decimal(18, 16) NULL,
	voltage decimal(6, 3) NULL,
	voltage_min decimal(6, 3) NULL,
	voltage_max decimal(6, 3) NULL,
	power decimal(18, 2) NULL,
	power_min decimal(18, 2) NULL,
	power_max decimal(18, 2) NULL,
	modTemperature decimal(4, 2) NULL,
	modTemperature_min decimal(4, 2) NULL,
	modTemperature_max decimal(4, 2) NULL,
	temperature decimal (4,2) null,
	temperature_min decimal (4,2) null,
	temperature_max decimal (4,2) null,
	insolation int NULL,
	insolation_min int NULL,
	insolation_max int NULL,
	windSpeed decimal(5, 2) NULL,
	windSpeed_min decimal(5, 2) NULL,
	windSpeed_max decimal(5, 2) NULL
)
;

 * 
 * 
 * 
 * 
 * 

 *********************
/* OLD SCRIPT .....
***********************
CREATE TABLE [dbo].[misc_northHillFarm](
	[dateMeasure] [dateTime] NOT NULL,
	[inverter] [varchar](50) NULL,
	[feedInTime] [decimal](18, 2) NULL,
	[operatingTime] [decimal](18, 2) NULL,
	[evtDescription] [varchar](255) NULL,
	[evtNo] [varchar](255) NULL,
	[evtNoShort] [varchar](4096) NULL,
	[evtMsg] [varchar](255) NULL,
	[evtPrior] [varchar](255) NULL,
	[isolationLeakRis] [decimal](18, 2) NULL,
	[gridSwitchCount] [decimal](18, 2) NULL,
	[health] [varchar](255) NULL
) ON [PRIMARY]
GO
select * from misc_northHillFarm where health != 'Ok'

insert into misc_northHillFarm
SELECT
       convert(DateTime, convert(varchar(10), dateMeasure) + ' ' + convert(varchar(5), timeMeasure) )
	  ,[inverter]
 	  ,[time_FeedIn]
	  ,[time_Operating]
      ,[evt_Description]
      ,[evt_No]
      ,[evt_NoShort]
      ,[evt_Msg]
      ,[evt_Prior]
      ,[isol_LeakRis]
      ,[gridSwitchCount]
      ,[health]
  FROM [solarPlants].[dbo].[m_northHillFarm]
  where stringNo is null

GO

CREATE TABLE [dbo].[mAc_northHillFarm](
	[dateMeasure] [dateTime] NOT NULL,
	[invPhase] [varchar](50) NULL,
	[current] [decimal](18, 2) NULL,
	[frequency] [decimal](18, 2) NULL,
	[voltage] [decimal](18, 2) NULL,
	[power] [decimal](18, 2) NULL,
	[energyDaily] [decimal](18, 2) NULL,
	[energyPrevious] [decimal](18, 2) NULL,
	[isolationFlt] [decimal](18, 2) NULL,
    generated as energyDaily - energyPrevious,
) ON [PRIMARY]

GO
USE [solarPlants]
GO

insert into mAc_northHillFarm
SELECT
       convert(DateTime, convert(varchar(10), dateMeasure) + ' ' + convert(varchar(5), timeMeasure) )
      ,[inverter]+case when stringNo = 'A' then 'Phase 1' when stringNo = 'B' then 'Phase 2' when stringNo = 'C' then 'Phase 3' when stringNo is null then '' else null END
      ,[out_Current]
      ,[out_Frequency]
      ,[out_Voltage]
      ,[out_ActivePower]
      ,[Out_EnergyDaily]
	  ,0
      ,[isol_Flt]
  FROM [solarPlants].[dbo].[m_northHillFarm]
  where stringNo is null or
  (stringNo = 'A' or stringNo = 'B' or stringNo = 'C')


update mAc_northHillFarm set energyPrevious =  
	(	select max(b.energyDaily) from mAc_northHillFarm b
		where  mAc_northHillFarm.invPhase = b.invPhase
		  and b.dateMeasure < mAc_northHillFarm.dateMeasure
		  and datediff(day, mAc_northHillFarm.dateMeasure, b.dateMeasure) <=1
		  and b.energyDaily <= mAc_northHillFarm.energyDaily
		  and mAc_northHillFarm.energyDaily - b.EnergyDaily < 10000
	)
where 
	invPhase not like '%Phase%' and
	energyPrevious is null



GO
CREATE TABLE [dbo].[dateLine](
	[dateTime] [dateTime] NOT NULL,
	[day]  AS (datepart(day,[dateTime])),
	[month]  AS (datepart(month,[dateTime])),
	[year]  AS (datepart(year,[dateTime])),
	[yearMonth]  AS (CONVERT([varchar](5),datepart(year,[dateTime]))+right('0'+CONVERT([varchar](2),datepart(month,[dateTime])),(2))),
	[semester]  AS (CONVERT([int],datepart(month,[dateTime])/(6.0)+(0.9))),
	[quarter]  AS (CONVERT([int],datepart(month,[dateTime])/(3.0)+(0.99))),
	[hour] as (datepart(hour, [dateTime])),
    YMD as convert(date, dateTime),
PRIMARY KEY CLUSTERED 
(
	[dateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
insert dateLine
select distinct convert(DateTime, convert(varchar(10), dateMeasure) + ' ' + convert(varchar(5), timeMeasure) ) from [solarPlants].[dbo].[m_northHillFarm]

  
 * 

drop table inverters
CREATE TABLE [dbo].[inverters](
	[company] [varchar](50) NULL,
	[site] [varchar](50) NOT NULL,
	[longitude] [decimal](9, 6) NULL,
	[latitude] [decimal](9, 6) NULL,
	city varchar(50) not null,
	country varchar(50) not null,
	power bigint not null,
	[mainPanel] [varchar](50) NULL,
	[distributionBoard] [varchar](50) NULL,
	[inverter] [varchar](50) NOT NULL,
	[invString] varchar(50) not null,
	stringNo varchar(50) null,
	[type] [tinyint] NULL,
    
PRIMARY KEY CLUSTERED 
(
	[invString] ASC, stringNo ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

 * 
insert inverters
select distinct  
	'SolarSW', 'northHillFarm', 55, -2, 'LONDON', 'UK', 15000, 
	'Panel 1', 'Board 1', inverter, inverter+case(stringNo) when 'C' then 'Phase 3' else stringNo END,
	case(stringNo) when 'C' then 'Phase 3' else stringNo END, 0
	from [solarPlants].[dbo].[m_northHillFarm]

insert inverters
select distinct  
	'SolarSW', 'northHillFarm', 55, -2, 'LONDON', 'UK', 15000, 
	'Panel 1', 'Board 1', inverter, inverter + 'Phase 1',
	'Phase 1', 0
	from [solarPlants].[dbo].[m_northHillFarm]
	where stringNo = 'C'

insert inverters
select distinct  
	'SolarSW', 'northHillFarm', 55, -2, 'LONDON', 'UK', 15000, 
	'Panel 1', 'Board 1', inverter, inverter + 'Phase 2',
	'Phase 2', 0
	from [solarPlants].[dbo].[m_northHillFarm]
	where stringNo = 'C'

 * 
 * get errors
select dateMeasure, inverter, evtNo = convert(int, LEFT(evtNo, CHARINDEX('.', evtNo)-1)), evtNoShort = convert(int, LEFT(evtNoShort, CHARINDEX('.', evtNoShort)-1)), evtMsg, evtPrior, health 
from misc_northHillFarm where 
	health != 'Ok' or 
	(evtNo is not null ) or 
	(evtMsg is not null and evtMsg != 'None') or 
	(evtNoShort is not null and evtNoShort != '0.00') or 
	(evtPrior is not null and evtPrior != 'NonePrio')
 order by dateMeasure DESC
  
 */


/*
drop table inverters;
select distinct inverter from m_northHillFarm;
create table inverters
(company varchar(50),
 site varchar(50) not null,
 longitude decimal(9, 6),
 latitude decimal (9, 6),
 mainPanel varchar(50),
 distributionBoard varchar(50),
 inverter varchar(50) primary key,
 isItWebBox tinyint)

insert into inverters
select distinct 'SolarSW', 'northHillFarm',  50.1234, -2.5678, 'Panel1', 'Board 1', inverter, 0 from m_northHillFarm
;
update inverters set isItWebBox=1 where inverter ='155025205'

 * 
 * 
 * drop table p_northHillFarm
;
create table p_northHillFarm
(datePrevi date not null,
 inverter varchar(50),
 isIsWebBox tinyint,
 out_ActivePower decimal(18,2)
 )

 insert into p_northHillFarm
 select dateMeasure, inverter, 0, sum(out_ActivePower) * (rand()+0.5) from m_northHillFarm
   group by  dateMeasure, inverter

   select * from p_northHillFarm order by datePrevi

   update p_northHillFarm set isIsWebBox = 1 where inverter = '155025205'

 alter table m_northHillFarm
add constraint FK_NorthHillFarmInverter foreign key(inverter)
  references inverters(inverter);

 * alter table p_northHillFarm
add constraint FK_NorthHillFarmInverterPrevi foreign key(inverter)
  references inverters(inverter);

 * alter table measures
add constraint FK_AllInverter foreign key(inverter)
  references inverters(inverter);

 * 
 * Idx:

create index idx_northHillFarm_date on m_northHillFarm(dateMeasure)
create index idx_measure_date on m_northHillFarm(inverter)
create index idx_measure_date on measures(dateMeasure)
create index idx_measure_inv on measures(inverter)
 
 
-------------------------
   load date/hour Line
-------------------------
drop table timeLine;

create table timeLine (
date date not null primary key,
day  as DAY(date),
month as month(date),
year as year(date),
yearMonth as cast( year(date) as varchar(5)) + right('0' + convert(varchar(2), month(date)),2),
semester as cast (month(date)/6.0 + 0.9 as int),
quarter as cast (month(date)/3.0 + 0.99 as int)
)
;
insert into timeline
select distinct dateMeasure from measures

select * from timeLine

drop table hourLine
create table hourLine(
	time time not null primary key,
	hour as cast( cast(time as varchar(2)) as integer),
	minute as cast( right(cast(time as varchar(5)), 2) as integer)
)

insert into hourLine
select distinct timeMeasure from measures

select * from hourLine


 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace loadData
{
    public class dbserver
    {
        String strConnect = "Data Source=.; Initial Catalog = solarSW ; Integrated Security = true ; Connection Timeout=10 ; Min Pool Size=2 ; Max Pool Size=20;";
        SqlConnection con;
        String[] tableNames = {
            "DC",
            "AC",
            "Misc"
        };
        public dbserver()
        {
            con = new SqlConnection(strConnect);
            con.Open();

        }
        public void switchSite(String siteName)
        {
            try
            {
                createTables(siteName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

//	[site] [varchar](50) not null,
         void createTables(String siteName)
        {

         }
        object getValueOrNull(String strVal)
         {
             try
             {
                 return String.IsNullOrWhiteSpace(strVal) ? (object)DBNull.Value : (object)Double.Parse(strVal.Replace(".",","));
             }
            catch(Exception ex)
             {
                Console.WriteLine("exception sur: " + strVal+ ", " + ex.Message);
                return (object) DBNull.Value;
            }
         }

        public Dictionary<string, inverter> loadInverters(String siteName)
        {
            Dictionary<String, inverter> allInvs = new Dictionary<string, inverter>();
            inverter oneInv;
            String serialNo, company, otherName, model, sensorSN;
            long sitePower, inverterPower;
            int type;
            invType invTp;

            using(SqlCommand cde = new SqlCommand(
                "select company, sitePower, serialNo, otherName, inverterPower, model, type, sensorSN "+
                " from inverter join site on siteName = name " +
                "where siteName = '"+ siteName+"';", con))
            {
                SqlDataReader myRdr = cde.ExecuteReader();
                while (myRdr.Read()) {
                    company = myRdr.GetString(0);
                    sitePower = myRdr.GetInt64(1);
                    serialNo = myRdr.GetString(2);
                    otherName = myRdr.GetString(3);
                    inverterPower = myRdr.GetInt64(4);
                    model = myRdr.GetString(5);
                    type = (int)myRdr.GetByte(6);
                    sensorSN = myRdr.GetString(7);
                    switch(type){
                        case 1:
                        invTp = invType.WEBBOX;
                        break;
                        case 2:
                        invTp = invType.SENSOR;
                        break;
                        default:
                        invTp = invType.INVERTER;
                        break;
                    }

                    oneInv = new inverter(company, siteName, sitePower, serialNo, otherName, inverterPower, model, invTp, sensorSN, true);
                    allInvs.Add(oneInv.serialNo, oneInv);
                }
                myRdr.Close();
            }
            return allInvs;
        }
        public void createMissingInverters(String siteName)
        {
            foreach(inverter inv in glob.inverters.Values){
                if (inv.bInDb)
                    continue;
                if (inv.siteName != siteName)
                    continue;
                using (SqlCommand cde = new SqlCommand(
                    "INSERT INTO [dbo].[inverter] (siteName, serialNo, otherName, inverterPower, model, type, sensorSN) VALUES(" +
                    "'" + siteName + "', '" + inv.serialNo + "', '" + inv.otherName + "', " + inv.inverterPower + "," +
                    "'" + inv.model + "', " + (int)inv.type + ", '" + inv.sensorSN + "');", con))
                        cde.ExecuteNonQuery();
                inv.bInDb = true;
            }
        }
        public void createSite(String siteName)
        {
            using (SqlCommand cde = new SqlCommand(
                "insert into site(company, name, longitude, latitude, city, country, sitePower) values (" +
                "'???', '" + siteName + "', 0, 0, '???', '???', 0);", con))
                cde.ExecuteNonQuery();
        }
        public void checkIfSiteExist(String siteName)
        {
            int rowCount;
            using (SqlCommand cde = new SqlCommand(
                "select count(*) from site where name = '" + siteName + "';", con))
                rowCount = (int)cde.ExecuteScalar();
            if (rowCount != 1)
                createSite(siteName);
        }
        public double getPreviousDaily(String siteName, String aString, DateTime dtMeasure, out DateTime previousDate)
        {
            previousDate = dtMeasure.AddMinutes(-5);
            // select top 1 daily from table, where siteName, aString, <drMesure totWh not null order by date Desc
            return 0;
        }
        public void prepareTable(String siteName)
        {
            glob.allTables = new List<DataTable>();
            for (int i = 0; i < 3; i++)
            {
                DataTable measuresDT = new DataTable("m_" + siteName + "_" + tableNames[i]);
                glob.allTables.Add(measuresDT);

                DataColumn myDC = new DataColumn("inverter", Type.GetType("System.String"));
                glob.allTables[i].Columns.Add(myDC);
                myDC = new DataColumn("aString", Type.GetType("System.String"));
                glob.allTables[i].Columns.Add(myDC);
                myDC = new DataColumn("dateMeasure", Type.GetType("System.DateTime"));
                glob.allTables[i].Columns.Add(myDC);

                foreach (measureDef mDef in glob.allM.Values)
                {
                    int tableTarget = 1 << i;
                    if ((mDef.tableMask & tableTarget) == tableTarget)
                    {
                        List<DataColumn> dateCol = new List<DataColumn>();
                        switch (mDef.valueDataType)
                        {
                            case valueDTP.INT:
                                dateCol.Add(new DataColumn(mDef.name, Type.GetType("System.Int32")));
                                break;
                            case valueDTP.DOUBLE:
                                dateCol.Add(new DataColumn(mDef.name, Type.GetType("System.Double")));
                                break;
                            case valueDTP.STRING:
                                dateCol.Add(new DataColumn(mDef.name, Type.GetType("System.String")));
                                break;
                            case valueDTP.M3INT:
                                dateCol.Add(new DataColumn(mDef.name, Type.GetType("System.Int32")));
                                dateCol.Add(new DataColumn(mDef.name+"_min", Type.GetType("System.Int32")));
                                dateCol.Add(new DataColumn(mDef.name+"_max", Type.GetType("System.Int32")));
                                break;
                            case valueDTP.M3DOUBLE:
                                dateCol.Add(new DataColumn(mDef.name, Type.GetType("System.Double")));
                                dateCol.Add(new DataColumn(mDef.name + "_min", Type.GetType("System.Double")));
                                dateCol.Add(new DataColumn(mDef.name + "_max", Type.GetType("System.Double")));
                                break;
                        }
                        foreach (DataColumn dc in dateCol)
                            glob.allTables[i].Columns.Add(dc);
                    }
                }
            }
        }
        public void prepareRow()
        {
        }
        public void loadRow(String inverterSN, String aString, DateTime dtM, Dictionary<String, int> measureInternalNameArray, List<object> vals){
            try
            {
                List<Boolean> bFlush = new List<Boolean>();
                glob.allRows = new List<DataRow>();

                for (int i = 0; i < 1; i++)
                {
                    bFlush.Add(false);
                    int tbMask = 1 << i;
                    glob.allRows.Add( glob.allTables[i].NewRow());
                    glob.allRows[i]["inverter"] = inverterSN;
                    glob.allRows[i]["aString"] = aString;
                    glob.allRows[i]["dateMeasure"] = dtM;

                    foreach (String internalMeasureName in measureInternalNameArray.Keys)
                    {
                        int indVal = measureInternalNameArray[internalMeasureName];
                        if (!measureInternalNameArray.ContainsKey(internalMeasureName))
                            throw new Exception("sql.loadrow: Measure <" + internalMeasureName + "> not found in allM");
                        measureDef mDef = glob.allM[internalMeasureName];
                        if (indVal >= vals.Count)
                            continue;

                        if ((mDef.tableMask & tbMask) == tbMask)
                        {
                            if (vals[indVal] == null)
                                continue;

                            if (mDef.bFlushDBRow)
                                bFlush[i] = true;

                            switch (mDef.valueDataType)
                            {
                                case valueDTP.INT:
                                case valueDTP.DOUBLE:
                                case valueDTP.STRING:
                                    glob.allRows[i][mDef.name] = vals[indVal];
                                    break;
                                case valueDTP.M3INT:
                                    int[] varIntArr = (int[])vals[indVal];
                                    glob.allRows[i][mDef.name] = varIntArr[0];
                                    glob.allRows[i][mDef.name + "_min"] = varIntArr[1];
                                    glob.allRows[i][mDef.name + "_max"] = varIntArr[2];
                                    break;
                                case valueDTP.M3DOUBLE:
                                    Double[] varDblArr = (Double[])vals[indVal];
                                    glob.allRows[i][mDef.name] = varDblArr[0];
                                    glob.allRows[i][mDef.name + "_min"] = varDblArr[1];
                                    glob.allRows[i][mDef.name + "_max"] = varDblArr[2];
                                    break;
                            }
                        }
                    }
                    if (i >= glob.allRows.Count)
                        if(bFlush[i])
                           glob.allRows.Add(null);
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        public void flushRow()
        {
            for (int i = 0; i < 1; i++)
            {
                int tbMask = 1 << i;
                if (glob.allRows[i] != null)
                    //if(glob.allTables[i].Rows.Count > 0)
                        glob.allTables[i].Rows.Add(glob.allRows[i]);
            }
        }
        public void writeToDb()
        {
            for (int i = 0; i < 1; i++)
            {
                if (glob.allTables[i].Rows.Count > 0)
                {
                    using (SqlConnection dbConnection = new SqlConnection(strConnect))
                    {
                        dbConnection.Open();
                        using (SqlBulkCopy s = new SqlBulkCopy(dbConnection))
                        {
                            s.DestinationTableName = glob.allTables[i].TableName;

                            foreach (var column in glob.allTables[i].Columns)
                                s.ColumnMappings.Add(column.ToString(), column.ToString());

                            s.WriteToServer(glob.allTables[i]);
                        }
                    }
                }
            }

        }

        /*
        public void loadData(String [][]measuresFromOneFile, Boolean[] bFlush, String siteName){

            Boolean bSomethingToWrite = false;
            Boolean bIsAWebBox = false;

            DataTable measuresDT = new DataTable("m_" + siteName);
            DataTable measuresDTWebBox = new DataTable("measures");

            initializeColumns(measuresDT, null);
            initializeColumns(measuresDTWebBox, siteName);


            foreach (String[] oneMeasurePerTimeInverterString in measuresFromOneFile)
            {
                bIsAWebBox = oneMeasurePerTimeInverterString[4].StartsWith("WebBox");

                int monTime = int.Parse(oneMeasurePerTimeInverterString[1].ToString().Substring(0, 2));

                // skip Measures before 7am or after 9pm, if no events
                if ((monTime >= 21 || monTime < 7) &&
                    (oneMeasurePerTimeInverterString[19] == null || 
                     oneMeasurePerTimeInverterString[19] == "None"))
                    continue;

                Boolean bToWrite = false;
                int iCol = -1;
                for (int jj = 0; jj < oneMeasurePerTimeInverterString.Length; jj++)
                {
                    String strCell = oneMeasurePerTimeInverterString[jj];
                    if (strCell != null && strCell.Length != 0)
                    {
                        if (bFlush[jj])
                            bToWrite = true;
                    }
                }
                if (bToWrite)
                {
                    DataRow dtRowMeasures = measuresDT.NewRow();
                    DataRow dtRowWebBox = measuresDTWebBox.NewRow();
                    loadRowValues(dtRowMeasures, oneMeasurePerTimeInverterString, null);
                    measuresDT.Rows.Add(dtRowMeasures);
                    if(bIsAWebBox){
                        loadRowValues(dtRowWebBox, oneMeasurePerTimeInverterString, siteName);
                        measuresDTWebBox.Rows.Add(dtRowWebBox);
                    }
                    bSomethingToWrite = true;
                }
            }
            if (bSomethingToWrite)
            {
                using (SqlConnection dbConnection = new SqlConnection(strConnect))
                {
                    dbConnection.Open();
                    using (SqlBulkCopy s = new SqlBulkCopy(dbConnection))
                    {
                        s.DestinationTableName = measuresDT.TableName;

                        foreach (var column in measuresDT.Columns)
                            s.ColumnMappings.Add(column.ToString(), column.ToString());

                        s.WriteToServer(measuresDT);
                    }
                    if(bIsAWebBox)
                    {
                        using (SqlBulkCopy s = new SqlBulkCopy(dbConnection))
                        {
                            s.DestinationTableName = measuresDTWebBox.TableName;

                            foreach (var column in measuresDTWebBox.Columns)
                                s.ColumnMappings.Add(column.ToString(), column.ToString());

                            s.WriteToServer(measuresDTWebBox);
                        }
                    }
                }
            }
        }
        void initializeColumns(DataTable dtT, String siteName)
        {
            if (siteName != null)
            {
                DataColumn siteCol = new DataColumn("site", Type.GetType("System.String"));
                dtT.Columns.Add(siteCol);
            }
            DataColumn dateCol = new DataColumn("dateMeasure", Type.GetType("System.String"));
            dtT.Columns.Add(dateCol);
            DataColumn timeColCol = new DataColumn("timeMeasure", Type.GetType("System.String"));
            dtT.Columns.Add(timeColCol);
            DataColumn inverterCol = new DataColumn("inverter", Type.GetType("System.String"));
            dtT.Columns.Add(inverterCol);
            DataColumn stringNoCol = new DataColumn("stringNo", Type.GetType("System.String"));
            dtT.Columns.Add(stringNoCol);

            DataColumn dc_CurrentCol = new DataColumn("dc_Current", Type.GetType("System.Double"));
            dtT.Columns.Add(dc_CurrentCol);
            DataColumn dc_VoltageCol = new DataColumn("dc_Voltage", Type.GetType("System.Double"));
            dtT.Columns.Add(dc_VoltageCol);
            DataColumn dc_PowerCol = new DataColumn("dc_Power", Type.GetType("System.Double"));
            dtT.Columns.Add(dc_PowerCol);
            DataColumn env_TempCol = new DataColumn("env_Temp", Type.GetType("System.Double"));
            dtT.Columns.Add(env_TempCol);
            DataColumn env_InsolationCol = new DataColumn("env_Insolation", Type.GetType("System.Double"));
            dtT.Columns.Add(env_InsolationCol);
            DataColumn env_WindSpeedCol = new DataColumn("env_WindSpeed", Type.GetType("System.Double"));
            dtT.Columns.Add(env_WindSpeedCol);
            DataColumn currentCol = new DataColumn("out_Current", Type.GetType("System.Double"));
            dtT.Columns.Add(currentCol);
            DataColumn frequencyCol = new DataColumn("out_Frequency", Type.GetType("System.Double"));
            dtT.Columns.Add(frequencyCol);
            DataColumn out_VoltageCol = new DataColumn("out_Voltage", Type.GetType("System.Double"));
            dtT.Columns.Add(out_VoltageCol);
            DataColumn out_ActivePowerCol = new DataColumn("out_ActivePower", Type.GetType("System.Double"));
            dtT.Columns.Add(out_ActivePowerCol);
            DataColumn mod_TempCol = new DataColumn("mod_Temp", Type.GetType("System.Double"));
            dtT.Columns.Add(mod_TempCol);
            DataColumn time_FeedInCol = new DataColumn("time_FeedIn", Type.GetType("System.Double"));
            dtT.Columns.Add(time_FeedInCol);
            DataColumn time_OperatingCol = new DataColumn("time_Operating", Type.GetType("System.Double"));
            dtT.Columns.Add(time_OperatingCol);
            DataColumn Out_EnergyDailyCol = new DataColumn("Out_EnergyDaily", Type.GetType("System.Double"));
            dtT.Columns.Add(Out_EnergyDailyCol);
            DataColumn evt_DescriptionCol = new DataColumn("evt_Description", Type.GetType("System.String"));
            dtT.Columns.Add(evt_DescriptionCol);
            DataColumn evt_NoCol = new DataColumn("evt_No", Type.GetType("System.String"));
            dtT.Columns.Add(evt_NoCol);
            DataColumn evt_NoShortCol = new DataColumn("evt_NoShort", Type.GetType("System.String"));
            dtT.Columns.Add(evt_NoShortCol);
            DataColumn evt_MsgCol = new DataColumn("evt_Msg", Type.GetType("System.String"));
            dtT.Columns.Add(evt_MsgCol);
            DataColumn evt_PriorCol = new DataColumn("evt_Prior", Type.GetType("System.String"));
            dtT.Columns.Add(evt_PriorCol);
            DataColumn isol_FltCol = new DataColumn("isol_Flt", Type.GetType("System.Double"));
            dtT.Columns.Add(isol_FltCol);
            DataColumn isol_LeakRisCol = new DataColumn("isol_LeakRis", Type.GetType("System.Double"));
            dtT.Columns.Add(isol_LeakRisCol);
            DataColumn gridSwitchCountCol = new DataColumn("gridSwitchCount", Type.GetType("System.Double"));
            dtT.Columns.Add(gridSwitchCountCol);
            DataColumn healthCol = new DataColumn("health", Type.GetType("System.String"));
            dtT.Columns.Add(healthCol);
        }
        void loadRowValues(DataRow dtR, String[] oneMeasurePerTimeInverterString, String siteName)
        {
            if (siteName != null)
            {
                dtR["site"] = siteName;
            }
            dtR["dateMeasure"] = oneMeasurePerTimeInverterString[0];
            dtR["TimeMeasure"] = oneMeasurePerTimeInverterString[1];
            dtR["inverter"] = oneMeasurePerTimeInverterString[2];
            dtR["stringNo"] = oneMeasurePerTimeInverterString[3];
            dtR["dc_Current"] = getValueOrNull(oneMeasurePerTimeInverterString[5]);
            dtR["dc_Voltage"] = getValueOrNull(oneMeasurePerTimeInverterString[6]);
            dtR["dc_Power"] = getValueOrNull(oneMeasurePerTimeInverterString[7]);
            dtR["env_Temp"] = getValueOrNull(oneMeasurePerTimeInverterString[8]);
            dtR["env_Insolation"] = getValueOrNull(oneMeasurePerTimeInverterString[9]);
            dtR["env_WindSpeed"] = getValueOrNull(oneMeasurePerTimeInverterString[10]);
            dtR["out_Current"] = getValueOrNull(oneMeasurePerTimeInverterString[11]);
            dtR["out_Frequency"] = getValueOrNull(oneMeasurePerTimeInverterString[12]);
            dtR["out_Voltage"] = getValueOrNull(oneMeasurePerTimeInverterString[13]);
            dtR["out_ActivePower"] = getValueOrNull(oneMeasurePerTimeInverterString[14]);
            dtR["mod_Temp"] = getValueOrNull(oneMeasurePerTimeInverterString[15]);
            dtR["time_FeedIn"] = getValueOrNull(oneMeasurePerTimeInverterString[16]);
            dtR["time_Operating"] = getValueOrNull(oneMeasurePerTimeInverterString[17]);
            dtR["Out_EnergyDaily"] = getValueOrNull(oneMeasurePerTimeInverterString[18]);
            dtR["evt_Description"] = oneMeasurePerTimeInverterString[19];
            dtR["evt_No"] = oneMeasurePerTimeInverterString[20];
            dtR["evt_NoShort"] = oneMeasurePerTimeInverterString[21];
            dtR["evt_Msg"] = oneMeasurePerTimeInverterString[22];
            dtR["evt_Prior"] = oneMeasurePerTimeInverterString[23];
            dtR["isol_Flt"] = getValueOrNull(oneMeasurePerTimeInverterString[24]);
            dtR["isol_LeakRis"] = getValueOrNull(oneMeasurePerTimeInverterString[25]);
            dtR["gridSwitchCount"] = getValueOrNull(oneMeasurePerTimeInverterString[26]);
            dtR["health"] = oneMeasurePerTimeInverterString[27];
        }
         * */
    }
}
