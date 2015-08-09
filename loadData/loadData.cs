using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using ReadWriteCsv;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using System.Data;

namespace loadData
{
    public class glob
    {
        static public Dictionary<String, inverter> inverters = new Dictionary<string, inverter>();
        static public Dictionary<String, measureDef> allM = new Dictionary<string, measureDef>();
        static public Dictionary<String, string> allMNames = new Dictionary<string, string>();
        static public ListBox lbResult;
        static public int MAX_VALUES = 30;
        static public dbserver dbs = new dbserver();
        static public List<DataTable> allTables = new List<DataTable>();
        static public List<DataRow> allRows = new List<DataRow>();

        private static glob _me = new glob();

        private glob() { ;}

        public static glob me()
        {
            return _me;
        }
        // utilities
        // 0 : root name
        // 1 : suffix 1
        // 2 : suffix 2
        // 3 : String
        // 4 : Original Name
        public static string[] decodeName(String mName)
        {
            String[] strName = new String[5];
            String[] tmpStr = mName.Split(new char[] { '.' });
            for (int ii = 0; ii < tmpStr.Count(); ii++)
                if(tmpStr[ii] != null && tmpStr[ii].Length >0)
                    tmpStr[ii] = tmpStr[ii].Trim();

            strName[4] = mName;
            strName[0] = tmpStr[0];
            if (tmpStr.Count() > 3)
                throw new Exception(mName + " has more than 2 dots !!!");
            if (tmpStr.Count() > 1)
                strName[1] = tmpStr[1];
            if (tmpStr.Count() > 2)
                strName[2] = tmpStr[2];
            int indToCheck = 1;
            if (strName[1] == null)
                indToCheck = 0;
            if (strName[indToCheck].IndexOf('[') > 0)
            {
                strName[3] = strName[indToCheck].Substring(strName[indToCheck].IndexOf('[') + 1, strName[indToCheck].IndexOf(']') - strName[indToCheck].IndexOf('[') - 1);
                strName[indToCheck] = strName[indToCheck].Substring(0, strName[indToCheck].IndexOf('['));
            }
            else
                strName[3] = "";
            if (strName[1] != null)
            {
                if (strName[1].ToLower().StartsWith("flt"))
                {
                    strName[3] = strName[1].Substring(3, 1);
                    strName[1] = strName[1].Substring(0, 3);
                }
            }
            if (strName[2] != null)
            {
                switch (strName[2].ToLower())
                {
                    case "phsa":
                        strName[3] = "Phase 1";
                        strName[2] = null;
                        break;
                    case "phsb":
                        strName[3] = "Phase 2";
                        strName[2] = null;
                        break;
                    case "phsc":
                        strName[3] = "Phase 3";
                        strName[2] = null;
                        break;
                    default:
                        if (strName[2].ToLower().StartsWith("phs"))
                            throw new Exception(mName + " has a phs suffice not reconized !!!");
                        break;
                }
            }
            return strName;
        }
        public static string getInternalName(String MeasureName)
        {
            string[] decodName = glob.decodeName(MeasureName);
            return glob.buildInternalName(decodName);
        }
        public static string buildInternalName(string[] dcdName){
            String strTmp = dcdName[0];
            if(dcdName[1] != null && dcdName[1].Length > 0)
                strTmp += "-" + dcdName[1];
            if(dcdName[2] != null && dcdName[2].Length > 0)
                strTmp += "-" + dcdName[2];
            return strTmp;
        }
    }
    class loadData
    {
        private int MAX_VALUES = glob.MAX_VALUES;
        private const int MAX_STRING = 10;
        
        // for dev 
        Boolean BOnlyOne = true;

        // Later we will put an alert on any file changes, and process only files with info later than the last datetime processed
        public void run(String strPath, String fileType, ListBox lb)
        {
            if (strPath == null || strPath.Length == 0)
                return;
            glob.lbResult = lb;

            // load and check measure information integrity
            loadAndCheckMeasureNames();

            String[] directories = Directory.GetDirectories(strPath, "*.*", SearchOption.TopDirectoryOnly);

            Directory.SetCurrentDirectory(strPath);
            foreach (String oneDir in directories)
            {
                Directory.SetCurrentDirectory(oneDir);
                String[] pathDetail = oneDir.Split(new char[] { '\\' });
                runForThisSite(pathDetail[pathDetail.Count() - 1], fileType);
            }
            Directory.SetCurrentDirectory(strPath);
        }

        public void runForThisSite(String siteName, String fileType)
        {
            Boolean bLoaded = false;
            glob.dbs.switchSite(siteName);

            String[] allFileNames = Directory.GetFiles(".", "2015-07*." + ((fileType == "xml") ? "zip" : fileType), SearchOption.AllDirectories);

            switch (fileType.ToLower())
            {
                case "csv":
                    foreach (String fileName in allFileNames)
                    {
                        if (!bLoaded)
                        {
                            glob.dbs.checkIfSiteExist(siteName);
                            glob.inverters = glob.dbs.loadInverters(siteName);
                            bLoaded = true;
                        }
                        //oneMeasure oneM = loadACsv(fileName);
                        //if (oneM.values.Count > 0)
                        //    glob.allM.Add(oneM);
                        //if (BOnlyOne)
                        //    break;
                    }
                    break;
                case "xml":
                    String fileNameRoot = "???";
                    foreach (String oneFile in allFileNames)
                    {
                        glob.lbResult.Items.Clear();
                        glob.lbResult.Items.Add("new file: " + oneFile);
                        if (!bLoaded)
                        {
                            glob.dbs.checkIfSiteExist(siteName);
                            glob.inverters = glob.dbs.loadInverters(siteName);
                            fileNameRoot = oneFile.Substring(1, 7); 
                            bLoaded = true;
                        }
                        String[] fnSplitted = oneFile.Split(new char[] { '\\' });
                        if (fileNameRoot != fnSplitted[fnSplitted.Count() -1 ].Substring(1, 7))
                        {
                            glob.lbResult.Items.Clear();
                            glob.lbResult.Items.Add("Saving data to the database");

                            saveToDb(siteName);
                            fileNameRoot = fnSplitted[fnSplitted.Count() - 1].Substring(1, 7);
                            glob.lbResult.Items.Clear();
                            glob.lbResult.Items.Add("new file: " + oneFile);
                        }
                        if (glob.lbResult.Items.Count > 50)
                        {
                            glob.lbResult.Items.Clear();
                            glob.lbResult.Items.Add("new file: " + oneFile);
                        }
                        glob.lbResult.Items.Add("Unzip: " + oneFile);
                        glob.lbResult.Refresh();
                        Application.DoEvents();
                        loadThisZipFile(siteName, oneFile);
                    }
                    if(bLoaded)
                        saveToDb(siteName);
                    break;
            }
        }
        private void saveToDb(String siteName)
        {
            // set default sensorBoxId
            glob.dbs.createMissingInverters(siteName);
            string sensorBoxId = null;
            foreach (inverter inv in glob.inverters.Values)
            {
                if (inv.type == invType.SENSOR)
                {
                    sensorBoxId = inv.serialNo;
                    break;
                }
            }
            if (sensorBoxId != null)
            {
                foreach (inverter inv in glob.inverters.Values)
                {
                    if (inv.sensorSN == null)
                        inv.sensorSN = sensorBoxId;
                }
            }
            // compute ratio
            foreach (inverter inv in glob.inverters.Values)
            {
                inv.computeRatios();
            }

            uploadData(siteName);
        }
        private void loadThisZipFile(String siteName, String oneFile){
            using (var file = File.OpenRead(oneFile))
            {
                try
                {
                    using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            //glob.lbResult.Items.Add("   file " + entry);
                            loadXml(siteName, entry.Open());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("File " + oneFile + " was not processed, Error: " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }
        public void loadXml(String siteName, Stream strm)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(strm);

            foreach (XmlNode xnd in xDoc.DocumentElement.ChildNodes)
                dumpANode(siteName, xnd);
        }
        void dumpANode(String siteName, XmlNode xnd)
        {
            string[] strInfo;
            DateTime dtMeasure;
            String strUnit;
            int measurePeriod;
            String[] measureNamesSplitted;
            inverter inv;
            String internalName;
            measureDef mDef;

            try
            {
                switch (xnd.Name)
                {
                    case "Info":

                        return;
                    case "CurrentPublic":
                    case "MeanPublic":
                        strInfo = getNodeInfo(xnd);
                        measureNamesSplitted = glob.decodeName(strInfo[2]);
                        dtMeasure = getNodeDate(xnd);
                        strUnit = getNodeValueString(xnd, "Unit");
                        measurePeriod = getNodeValInt(xnd, "Period");

                        if (!glob.inverters.ContainsKey(strInfo[1]))
                        {
                            inv = new inverter("???", siteName, 0, strInfo[1], strInfo[0], 0, "???");
                            glob.inverters.Add(inv.serialNo, inv);
                        }
                        else
                            inv = glob.inverters[strInfo[1]];
                        if (glob.allMNames.ContainsKey(strInfo[2]))
                        {
                            internalName = glob.allMNames[strInfo[2]];
                        }
                        else
                        {
                            Console.WriteLine("Measure: " + strInfo[2] + " not found in allMNames");
                            return;
                        }
                        if (internalName == null)
                            return;
                        if (!glob.allM.ContainsKey(internalName))
                        {
                            MessageBox.Show ("No measure definition for " + strInfo[2] + "("+internalName+")");
                            return;
                        }
                        mDef = glob.allM[internalName];
                        switch (mDef.valueDataType)
                        {
                            case valueDTP.INT:
                                inv.setValue(dtMeasure, measureNamesSplitted[3], strInfo[2], getNodeValInt(xnd, "Mean"));
                                break;
                            case valueDTP.DOUBLE:
                                inv.setValue(dtMeasure, measureNamesSplitted[3], strInfo[2], getNodeValDouble(xnd, "Mean"));
                                break;
                            case valueDTP.STRING:
                                inv.setValue(dtMeasure, measureNamesSplitted[3], strInfo[2], getNodeValueString(xnd, "Mean"));
                                break;
                            case valueDTP.M3INT:
                                int[] valIntArray = new int[3];
                                valIntArray[0] = getNodeValInt(xnd, "Mean");
                                valIntArray[1] = getNodeValInt(xnd, "Min");
                                valIntArray[2] = getNodeValInt(xnd, "Max");

                                inv.setValue(dtMeasure, measureNamesSplitted[3], strInfo[2], valIntArray);

                                break;
                            case valueDTP.M3DOUBLE:
                                double[] valDoubleArray = new Double[3];
                                valDoubleArray[0] = getNodeValDouble(xnd, "Mean");
                                valDoubleArray[1] = getNodeValDouble(xnd, "Min");
                                valDoubleArray[2] = getNodeValDouble(xnd, "Max");

                                inv.setValue(dtMeasure, measureNamesSplitted[3], strInfo[2], valDoubleArray);

                                break;
                        }

                        break;
                    default:
                        MessageBox.Show("Site: " + siteName + ", Name of node unknown: " + xnd.Name + ": " + xnd.InnerText);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void uploadData(string siteName)
        {
            glob.dbs.prepareTable(siteName);
            foreach (inverter inv in glob.inverters.Values)
            {
                stringVal strVal = inv.values;
                // for all strings of this inverter
                for (int iStrVal = 0; iStrVal < strVal.dVals.Count; iStrVal++)
                {
                    String aString = strVal.stringNames[iStrVal];
                    dateVal dtVal = strVal.dVals[iStrVal];

                    // for all dates of this string
                    foreach (DateTime aDate in dtVal.valsPerDate.Keys)
                    {
                        valueVal valVal = dtVal.valsPerDate[aDate];
                        List<object> objVal = (List<object>)valVal.val;
                        glob.dbs.loadRow(inv.serialNo, aString, aDate, inv.measureInternalNameArray, objVal);
                        glob.dbs.flushRow();
                    }
                }
                glob.dbs.writeToDb();
                glob.dbs.prepareTable(siteName);
            }
            glob.inverters = glob.dbs.loadInverters(siteName);
        }
        int getNodeValInt(XmlNode xnd, String key)
        {
            int ii = 0;
            string strTmp = getNodeValueString(xnd, key);
            if (strTmp == null || strTmp.Length == 0 || int.TryParse(strTmp, out ii))
                return ii;

            throw new Exception("invalid integer: <" + strTmp + ">, node info: " + xnd.InnerText);
        }
        double getNodeValDouble(XmlNode xnd, String key)
        {
            double ii = 0;
            string strTmp = getNodeValueString(xnd, key).Replace(".", ",");
            if (strTmp == null || strTmp.Length == 0 || Double.TryParse(strTmp, out ii))
                return ii;

            throw new Exception("invalid double value: <" + strTmp + ">, node info: " + xnd.InnerText);
        }
        DateTime getNodeDate(XmlNode xnd)
        {
            DateTime dt;

            String strTmp = getNodeValueString(xnd, "Timestamp");
            if (DateTime.TryParse(strTmp, out dt))
                return dt;

            throw new Exception("Invalid Date: <" + strTmp + ">, node Info: " + xnd.InnerText);
        }
        // 0: SN, 1: name, 2: serialNo, 3: measureKey
        String[] getNodeInfo(XmlNode xnd)
        {
            String[] strRet;

            String tmpStr = getNodeValueString(xnd, "Key");
            strRet = tmpStr.Split(new char[] { ':' });
            if (strRet.Count() == 5)
            {
                String[] tmpStrArr = new String[3];
                tmpStrArr[0] = strRet[0] + ":" + strRet[1] + ":" + strRet[2];
                tmpStrArr[1] = strRet[3];
                tmpStrArr[2] = strRet[4];
                strRet = tmpStrArr;
            }
            if (strRet.Count() == 4)
            {
                String[] tmpStrArr = new String[3];
                tmpStrArr[0] = strRet[0] + ":" + strRet[1];
                tmpStrArr[1] = strRet[2];
                tmpStrArr[2] = strRet[3];
                strRet = tmpStrArr;
            }
            if (strRet.Count() != 3)
                throw new Exception("invalid node information: " + xnd.OuterXml);
            for (int ii = 0; ii < strRet.Count(); ii++)
                if (strRet[ii] != null && strRet[ii].Length > 0)
                    strRet[ii] = strRet[ii].Trim();
            return strRet;
        }
        String getNodeValueString(XmlNode xnd, String key)
        {
            string strVal = "";
            foreach (XmlNode xx in xnd.ChildNodes)
            {
                if (xx.Name == key)
                {
                    return xx.InnerText;
                }
            }
            if(key != "Unit")
                MessageBox.Show("Key: " + key + " not found in node: " + xnd.OuterXml);

            return null;
        }
        public void loadAndCheckMeasureNames()
        {
            // mapping real names, internal names
            Dictionary<String, int> redirectNames = new Dictionary<string, int>()
            {
                {"DcMs.Amp[A]", 1},
                {"DcMs.Amp[A1]", 1},
                {"DcMs.Amp[A2]", 1},
                {"DcMs.Amp[A3]", 1},
                {"DcMs.Amp[A4]", 1},
                {"DcMs.Amp[A5]", 1},
                {"DcMs.Amp[B]", 1},
                {"DcMs.Amp[B1]", 1},
                {"DcMs.Vol[A]", 2},
                {"DcMs.Vol[B]", 2},
                {"DcMs.Watt[A]", 3},
                {"DcMs.Watt[B]", 3},
                {"Env.TmpVal", 4},
                {"Env.TotInsol", 5},
                {"Env.HorWSpd", 6},
                {"GridMs.A.phsA", 7},
                {"GridMs.A.phsB", 7},
                {"GridMs.A.phsC", 7},
                {"GridMs.Hz", 8},
                {"GridMs.PhV.phsA", 9},
                {"GridMs.PhV.phsB", 9},
                {"GridMs.PhV.phsC", 9},
                {"GridMs.TotW", 10},
                {"Mdul.TmpVal", 11},
                {"Metering.TotFeedTms", 12},
                {"Metering.TotOpTms", 13},
                {"Metering.TotWhOut", 14}, 
                {"Operation.Evt.Dsc", 15}, 
                {"Operation.Evt.No", 16}, 
                {"Operation.Evt.NoShrt", 17}, 
                {"Operation.Evt.Msg", 18}, 
                {"Operation.Evt.Prio", 19}, 
                {"Isolation.FltA", 20}, 
                {"Isolation.LeakRis", 21}, 
                {"Operation.GriSwCnt", 22}, 
                {"Operation.Health", 23},
                
                // mapping for computed values
                {"Computed.generated", 24},
                {"Computed.perf.ratio", 25},

                // mapping for name errors
                {"6627584", -1},    //1  "6627584[A1]"
                {"4529920",-1},     //2
                {"2432512",-1},     //3
                {"4609792",-1},
                {"2506496",-1},
                {"4608512",-1},
                {"4608256",-1},
                {"4608000",-1},
                {"4606464",-1},
                {"4606208",-1},
                {"4605952",-1},
                {"4530432",-1}
            };
            List<measureDef> measuresDef = new List<measureDef>()
            {
                new measureDef ("emptySolt", "NeverComeHere", "??", "", 0, "?", false, valueDTP.STRING, 0),
                new measureDef ("currentAmp", "DcMs", "Amp", "", 1, "A", true, valueDTP.DOUBLE, 1),
                new measureDef ("voltage","DcMs", "Vol", "", 2, "V", true, valueDTP.M3DOUBLE, 1),
                new measureDef ("power", "DcMs", "Watt", "", 3, "W", true, valueDTP.M3DOUBLE, 1),
                new measureDef ("temperature", "Env", "TmpVal", "", 4, "C", true, valueDTP.M3DOUBLE, 3),
                new measureDef ("insolation", "Env", "TotInsol", "", 5, "w/m2", true, valueDTP.M3INT, 3),
                new measureDef ("windspeed", "Env", "HorWSpd", "", 6, "m/s", true, valueDTP.M3DOUBLE, 4),
                new measureDef ("current", "GridMs", "A", "", 7, "A", true, valueDTP.M3DOUBLE, 2),
                new measureDef ("frequency", "GridMs", "Hz", "", 8, "Hz", true, valueDTP.M3DOUBLE, 2),
                new measureDef ("voltage", "GridMs", "PhV", "", 9, "V", true, valueDTP.M3DOUBLE, 2),
                new measureDef ("activePower", "GridMs", "TotW", "", 10, "W", true, valueDTP.M3DOUBLE, 2),
                new measureDef ("modTemperature", "Mdul", "TmpVal", "", 11, "C", false, valueDTP.M3DOUBLE, 3),
                new measureDef ("feedInTime", "Metering", "TotFeedTms", "", 12, "?", false, valueDTP.DOUBLE, 2),
                new measureDef ("OperatingTime", "Metering", "TotOpTms", "", 13, "h", false, valueDTP.DOUBLE, 2),
                new measureDef ("energyDaily", "Metering", "TotWhOut", "", 14, "Wh", true, valueDTP.DOUBLE, 3),
                new measureDef ("evtDescription", "Operation", "Evt", "Dsc", 15, "String", false, valueDTP.STRING, 4),
                new measureDef ("evtNo", "Operation", "Evt", "No", 16, "String", true, valueDTP.STRING, 4),
                new measureDef ("evtNoShort", "Operation", "Evt", "NoShrt", 17, "String", false, valueDTP.STRING, 4),
                new measureDef ("evtMessage", "Operation", "Evt", "Msg", 18, "String", true, valueDTP.STRING, 4),
                new measureDef ("evtPrior", "Operation", "Evt", "Prio", 19, "String", false, valueDTP.STRING, 4),
                new measureDef ("isolationFlt", "Isolation", "Flt", "", 20, "String", false, valueDTP.M3DOUBLE, 4),
                new measureDef ("isolationLeakRis", "Isolation", "LeakRis", "", 21, "String", false, valueDTP.M3DOUBLE, 4),
                new measureDef ("gridSwitchCount", "Operation", "GriSwCnt", "", 22, "String", false, valueDTP.DOUBLE, 4),
                new measureDef ("health", "Operation", "Health", "", 23, "String", false, valueDTP.STRING, 4),
                new measureDef ("generated", "Computed", "generated", "", 24, "kWh", false, valueDTP.DOUBLE, 3),
                new measureDef ("perfRatio", "Computed", "perf", "ratio", 25, "%", false, valueDTP.DOUBLE, 3)
            };

            foreach (measureDef mDef in measuresDef)
            {
                glob.allM.Add(mDef.internalName, mDef);
            }
            foreach (string strRedir in redirectNames.Keys)
            {
                string intName = null;
                if (redirectNames[strRedir] != -1)
                {
                    intName = glob.getInternalName(strRedir);

                    if (!glob.allM.ContainsKey(intName))
                    {
                        throw new Exception("error in measureDef: " + strRedir + ": " + intName + " is not found in glob.allM array");
                    }
                }
                glob.allMNames.Add(strRedir, intName);
            }
        }
    }
        public class measureDef
        {
            public string name;
            public string internalName;
            public string[] nameDecoded; // 0 root, 1 suffix1, 2 suffix2
            public int id;
            public string unit;
            public bool bFlushDBRow;
            public valueDTP valueDataType;
            public int tableMask;

            public measureDef(string name, string root, string suffix1, string suffix2, int id, string unit, bool bFlushDBRow, valueDTP valueDataType, int tableMask)
            {

                this.name = name;
                this.nameDecoded = new string[3];
                this.nameDecoded[0] = root;
                this.nameDecoded[1] = suffix1;
                this.nameDecoded[2] = suffix2;
                this.internalName = glob.buildInternalName(this.nameDecoded);
                this.id = id;
                this.unit = unit;
                this.bFlushDBRow = bFlushDBRow;
                this.valueDataType = valueDataType;
                this.tableMask = tableMask;
            }
        }

    /// <summary>
    ///     VALUEADD
    /// </summary>
    public enum valueDTP { INT = 1, DOUBLE = 2, STRING = 3, M3INT = 10, M3DOUBLE = 11 };
    public class valueVal
    {
        public object val;

        public valueVal()
        {
            val = (object) new List<object>(glob.MAX_VALUES);
        }
        public void checkSlot(int no)
        {
            List<object> valObj = (List<object>)val;
            while (no >= valObj.Count)
                valObj.Add(null);
        }
        public object getValue(int no)
        {
            checkSlot(no);
            List<object> valObj = (List<object>)val;
            return valObj[no];
        }
        public void setValue(int no, object value)
        {
            checkSlot(no);
            List<object> valObj = (List<object>)val;
            valObj[no] = value;
        }
    }

    /// <summary>
    /// DATEVAL
    /// </summary>
    public class dateVal
    {
        public SortedDictionary<DateTime, valueVal> valsPerDate;

        public dateVal()
        {
            valsPerDate = new SortedDictionary<DateTime, valueVal>();
        }
        DateTime getDateIdx(DateTime dt)
        {
            if (!valsPerDate.ContainsKey(dt))
                valsPerDate.Add(dt, new valueVal());
            return dt;
        }
        public object getValue(DateTime dt, int no)
        {
            return valsPerDate[getDateIdx(dt)].getValue(no);
        }
        public void setValue(DateTime dt, int no, object value)
        {
            valsPerDate[getDateIdx(dt)].setValue(no, value);
        }
    }
    /// <summary>
    /// STRINGVAL
    /// </summary>
    public class stringVal
    {
        public List<string> stringNames;
        public List<dateVal> dVals;

        public stringVal()
        {
            stringNames = new List<string>();
            dVals = new List<dateVal>();
        }
        int getStringNo(String str)
        {
            for (int i = 0; i < stringNames.Count(); i++)
            {
                if (this.stringNames[i] == str)
                    return i;
            }
            this.stringNames.Add(str);
            int noStr = getStringNo(str);
            dVals.Add(new dateVal());
            if (dVals.Count != stringNames.Count)
                throw new Exception("stringVal.getStrinNo: internal inconsistency");
            return noStr;
        }
        public object getValue(DateTime dt, string str, int no)
        {
            return dVals[getStringNo(str)].getValue(dt, no);
        }
        public void setValue(DateTime dt, string str, int no, object value)
        {
            dVals[getStringNo(str)].setValue(dt, no, value);
        }
    }
    /// <summary>
    /// INVERTER
    /// </summary>
    public enum invType { WEBBOX = 1, SENSOR = 2, INVERTER = 3 };
    public enum valueType { MEAN = 0, MIN = 1, MAX = 2, UNDEFINED=99 };
    public class inverter
    {
        public String company;
        public String siteName;
        public long sitePower;
        public String serialNo;
        public String otherName;
        public long inverterPower;
        public String model;
        public invType type;
        public String sensorSN;
        public Boolean bInDb;

        public List<valueDTP> dataTP;
        public Dictionary<String, int> measureInternalNameArray;        // internal, colNber
        public stringVal values;        // dateVal, StringVal, ValueVal
        
        public inverter(String company, String siteName, long sitePower, String serialNo, String otherName, long inverterPower, 
                        String model, invType type = invType.INVERTER, String sensorSN = null, Boolean bInDBFlag = false)
        {
            this.company = company;
            this.siteName = siteName;
            this.sitePower = sitePower;
            this.serialNo = serialNo;
            this.otherName = otherName;
            this.inverterPower=inverterPower;
            this.model = model;
            this.type = type;
            this.sensorSN = sensorSN;
            this.bInDb = bInDBFlag;

            this.dataTP = new List<valueDTP>();
            this.measureInternalNameArray = new Dictionary<string, int>();     // internalNames
            if (inverterPower == 0)
            {
                string output = new string(model.Where(c => char.IsDigit(c)).ToArray());
                inverterPower = 0;
                long.TryParse(output, out inverterPower);
            }
            if (this.otherName.ToLower().Contains("webbox"))
                this.type = invType.WEBBOX;
            else
                if (this.otherName.ToLower().Contains("sensor"))
                    this.type = invType.SENSOR;
            this.values = new stringVal();
        }
        int getColNo(DateTime mDate, String measureName, String str)
        {
            string internalName = glob.getInternalName(measureName);

            // check in the measure is mapped
            if (glob.allMNames.ContainsKey(measureName))
                internalName = glob.allMNames[measureName];

            // do we already encountered this measure ?
            if(measureInternalNameArray.ContainsKey(internalName)){
                return measureInternalNameArray[internalName];
            }

            // we need to add a new value slot for our new measure
            measureDef mDef = glob.allM[internalName];
            if (mDef == null)
                throw new Exception("measure: " + measureName + "(internal: " + internalName + ") not found in glob.allM");

            if(! glob.allM.ContainsKey(internalName)){
                // no matching mesure found in our catalog !!!
                throw new Exception ("inverter.getColNo: measure: " + measureName + "(" + internalName + ") not found in allMNames neither allM");
            }

            this.dataTP.Add(mDef.valueDataType);
            measureInternalNameArray.Add(internalName, this.dataTP.Count-1);

            if (this.measureInternalNameArray.Count != this.dataTP.Count)
                throw new Exception("Inverter: inconsitency between dataTP and measureInternal: " + dataTP.Count.ToString() + ", " + measureInternalNameArray.Count.ToString());

            return this.dataTP.Count -1;
        }
        public object getValue(DateTime dt, string str, string measureName, valueType valueType = valueType.MEAN)
        {
            return getValue(dt, str, getColNo(dt, measureName, str), valueType);
        }
        public object getValue(DateTime dt, string str, int no, valueType valueType = valueType.MEAN)
        {
            if (no > dataTP.Count - 1)
                throw new Exception("inverter.getValue: invalid index");

            object valObj = values.getValue(dt, str, no);
            
            switch (dataTP[no])
            {
                case valueDTP.INT:
                case valueDTP.DOUBLE:
                case valueDTP.STRING:
                    return valObj;
                default:
                    List<object> valObjs = (List<object>)valObj;
                    return (object) valObjs [(int) valueType];
            }
            throw new Exception ("inverter.getValue: why am I here ??");
        }
        public void setValue(DateTime dt, string str, string measureName, int value)
        {
            setValue(dt, str, getColNo(dt, measureName, str), value);
        }
        public void setValue(DateTime dt, string str, int no, int value)
        {
            values.setValue(dt, str, no, value);
        }
        public void setValue(DateTime dt, string str, string measureName, double value)
        {
            setValue(dt, str, getColNo(dt, measureName, str), value);
        }
        public void setValue(DateTime dt, string str, int no, double value)
        {
            values.setValue(dt, str, no, value);
        }
        public void setValue(DateTime dt, string str, string measureName, string value)
        {
            setValue(dt, str, getColNo(dt, measureName, str), value);
        }
        public void setValue(DateTime dt, string str, int no, string value)
        {
            values.setValue(dt, str, no, value);
        }
        public void setValue(DateTime dt, string str, string measureName, int[] value)
        {
            setValue(dt, str, getColNo(dt, measureName, str), value);
        }
        public void setValue(DateTime dt, string str, int no, int[] value)
        {
            values.setValue(dt, str, no, value);
        }
        public void setValue(DateTime dt, string str, string measureName, double[] value)
        {
            setValue(dt, str, getColNo(dt, measureName, str), value);
        }
        public void setValue(DateTime dt, string str, int no, double[] value)
        {
            values.setValue(dt, str, no, value);
        }
        public void computeRatios()
        {
            try
            {
                double previousTotWhOut;
                double currentTotWhOut;
                DateTime previousDate;
                double generated;
                double perfRatio;
                int dailyCol, geneCol = -1, ratioCol = -1;

                // Metering.TotWhOut
                if (this.type != invType.INVERTER)
                    return;
                stringVal strVal = this.values;
                if (!measureInternalNameArray.ContainsKey("Metering-TotWhOut"))
                    return;
                dailyCol = measureInternalNameArray["Metering-TotWhOut"];
                for (int iStrVal = 0; iStrVal < strVal.dVals.Count; iStrVal++)
                {
                    String aString = strVal.stringNames[iStrVal];
                    dateVal dtVal = strVal.dVals[iStrVal];
                    if (dtVal.valsPerDate == null || dtVal.valsPerDate.Count == 0)
                        continue;
                    DateTime dtFirst = dtVal.valsPerDate.Keys.First();
                    previousTotWhOut = glob.dbs.getPreviousDaily(siteName, aString, dtFirst, out previousDate);

                    foreach (DateTime aDate in dtVal.valsPerDate.Keys)
                    {
                        if (aDate == dtFirst)
                        {
                            geneCol = getColNo(aDate, "Computed.generated", aString);
                            ratioCol = getColNo(aDate, "Computed.perf.ratio", aString);
                        }
                        valueVal valVal = dtVal.valsPerDate[aDate];

                        List<object> lstVal = (List<object>)valVal.val;
                        if (lstVal == null || dailyCol >= lstVal.Count || lstVal[dailyCol] == null)
                            continue;

                        currentTotWhOut = (Double)lstVal[dailyCol];

                        switch (validateDailyHisto(previousDate, aDate, previousTotWhOut, currentTotWhOut))
                        {
                            case histoValidation.OK:
                                generated = currentTotWhOut - previousTotWhOut;
                                this.values.setValue(aDate, aString, geneCol, (object)generated);
                                break;
                            case histoValidation.IGNORE_MEASURE:
                                break;
                            case histoValidation.RESET_VALUE:
                                this.values.setValue(aDate, aString, geneCol, (object)0);
                                break;
                        }
                        previousDate = aDate;
                        previousTotWhOut = currentTotWhOut;

                        // compute perf ratio
                        Random rd = new Random(DateTime.Now.Millisecond);

                        perfRatio = rd.Next(60, 100);
                        this.values.setValue(aDate, aString, ratioCol, perfRatio);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        enum histoValidation {OK, IGNORE_MEASURE, RESET_VALUE};
        private histoValidation validateDailyHisto(DateTime prevDt, DateTime curDt, Double prevDaily, Double curDaily)
        {
            if (curDaily < prevDaily)
                return histoValidation.RESET_VALUE;
            if (curDaily > (prevDaily + 100))
                return histoValidation.RESET_VALUE;
            return histoValidation.OK;
        }
    }
}

