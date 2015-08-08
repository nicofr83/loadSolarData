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

namespace loadData
{
    public class glob
    {
        static public Dictionary<String, inverter> inverters = new Dictionary<string, inverter>();
        static public Dictionary<String, measureDef> allM = new Dictionary<string, measureDef>();
        static public Dictionary<String, string> allMNames = new Dictionary<string, string>();
        static public ListBox lbResult;
        static public int MAX_VALUES = 30;

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
        private int MAX_VALUES_PER_LINE = 150;
        private int MAX_VALUES = glob.MAX_VALUES;
        private const int MAX_STRING = 10;
        dbserver dbs;
        private Boolean[] bFlushMaster;
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
            dbs = new dbserver(siteName);
            loadAndCheckInverters(siteName);

            String[] allFileNames = Directory.GetFiles(".", "*." + ((fileType == "xml") ? "zip" : fileType), SearchOption.AllDirectories);

            switch (fileType.ToLower())
            {
                case "csv":
                    foreach (String fileName in allFileNames)
                    {
                        //oneMeasure oneM = loadACsv(fileName);
                        //if (oneM.values.Count > 0)
                        //    glob.allM.Add(oneM);
                        //if (BOnlyOne)
                        //    break;
                    }
                    break;
                case "xml":
                    foreach (String oneFile in allFileNames)
                    {
                        using (var file = File.OpenRead(oneFile))
                        {
                            try
                            {
                                using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
                                {
                                    foreach (var entry in zip.Entries)
                                    {
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

                    break;
            }

            //dbs.checkInverters(siteName, glob.allM);
            //dbs.checkDateTimeLine(glob.allM);

            //processData(glob.allM);

            //uploadData(siteName);

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
                            inv = new inverter(siteName, strInfo[0], "???", strInfo[1]);
                            glob.inverters.Add(inv.serialNo, inv);
                        }
                        else
                            inv = glob.inverters[strInfo[1]];
                        internalName = glob.allMNames[strInfo[2]];
                        if (internalName == null)
                            internalName = glob.buildInternalName(measureNamesSplitted);
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
                // mapping for name errors
                {"6627584", -1},    //1
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
                new measureDef ("emptySolt", "NeverComeHere", "??", "", 0, "?", false, valueDTP.STRING),
                new measureDef ("dc_Current", "DcMs", "Amp", "", 1, "A", true, valueDTP.DOUBLE),
                new measureDef ("dc_Voltage","DcMs", "Vol", "", 2, "V", true, valueDTP.M3DOUBLE),
                new measureDef ("dc_Power", "DcMs", "Watt", "", 3, "W", true, valueDTP.M3DOUBLE),
                new measureDef ("environmentTemp", "Env", "TmpVal", "", 4, "C", true, valueDTP.M3DOUBLE),
                new measureDef ("envirnomentTotalInsolation", "Env", "TotInsol", "", 5, "w/m2", true, valueDTP.M3DOUBLE),
                new measureDef ("envirnomentWindspeed", "Env", "HorWSpd", "", 6, "m/s", true, valueDTP.M3DOUBLE),
                new measureDef ("current", "GridMs", "A", "", 7, "A", true, valueDTP.M3DOUBLE),
                new measureDef ("frequency", "GridMs", "Hz", "", 8, "Hz", true, valueDTP.M3DOUBLE),
                new measureDef ("voltage", "GridMs", "PhV", "", 9, "V", true, valueDTP.M3DOUBLE),
                new measureDef ("activePower", "GridMs", "TotW", "", 10, "W", true, valueDTP.M3DOUBLE),
                new measureDef ("mod_Temp", "Mdul", "TmpVal", "", 11, "C", false, valueDTP.M3DOUBLE),
                new measureDef ("time_FeedIn", "Metering", "TotFeedTms", "", 12, "?", false, valueDTP.DOUBLE),
                new measureDef ("time_Operating", "Metering", "TotOpTms", "", 13, "h", false, valueDTP.DOUBLE),
                new measureDef ("Out_EnergyDaily", "Metering", "TotWhOut", "", 14, "W", true, valueDTP.DOUBLE),
                new measureDef ("evt_Description", "Operation", "Evt", "Dsc", 15, "String", false, valueDTP.STRING),
                new measureDef ("evt_No", "Operation", "Evt", "No", 16, "String", true, valueDTP.STRING),
                new measureDef ("evt_NoShort", "Operation", "Evt", "NoShrt", 17, "String", false, valueDTP.STRING),
                new measureDef ("evt_Msg", "Operation", "Evt", "Msg", 18, "String", true, valueDTP.STRING),
                new measureDef ("evt_Prior", "Operation", "Evt", "Prio", 19, "String", false, valueDTP.STRING),
                new measureDef ("isol_Flt", "Isolation", "Flt", "", 20, "String", false, valueDTP.M3DOUBLE),
                new measureDef ("isol_LeakRis", "Isolation", "LeakRis", "", 21, "String", false, valueDTP.M3DOUBLE),
                new measureDef ("gridSwitchCount", "Operation", "GriSwCnt", "", 22, "String", false, valueDTP.DOUBLE),
                new measureDef ("Health", "Operation", "Health", "", 23, "String", false, valueDTP.STRING) 
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
        public void loadAndCheckInverters(String siteName)
        {
            glob.inverters = dbs.loadInverters(siteName);
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

            public measureDef(string name, string root, string suffix1, string suffix2, int id, string unit, bool bFlushDBRow, valueDTP valueDataType)
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
            }
        }

    /// <summary>
    ///     VALUEADD
    /// </summary>
    public enum valueDTP { INT = 1, DOUBLE = 2, STRING = 3, M3INT = 10, M3DOUBLE = 11 };
    public class valueVal
    {
        // indice = colNo defined in inverter
        List<valueDTP> dType;
        public object val;

        public valueVal()
        {
            dType = new List<valueDTP>(glob.MAX_VALUES);
            val = (object) new List<object>(glob.MAX_VALUES);
        }
        public Boolean isMeasureExist(int no, valueDTP dataType)
        {
            List<object> valObj = (List<object>)val;
            if (no >= valObj.Count)
                return false;
            if ( dType[no] != dataType)
                throw new Exception("bad data value type");

            return true;
        }
        public void checkSlot(int no, valueDTP valueDataType)
        {
            if (no < dType.Count)
                return;
            if (no != dType.Count)
                throw new Exception("creation of multiple slots... ???");

            dType.Add(valueDataType);
            List<object> valObj = (List<object>)val;
            valObj.Add(null);

            if (dType.Count != valObj.Count())
                throw new Exception("measureAdd: internal inconsistency");
        }

        public int measureAdd(valueDTP dtypeMeasure)
        {
            // Add value
            dType.Add(dtypeMeasure);
            // add value
            List<object> valObj = (List<object>)val;
            valObj.Add(null);

            if (dType.Count != valObj.Count())
                throw new Exception("measureAdd: internal inconsistency");
            return dType.Count-1;
        }
        void checkNo(int no)
        {
            List<object> objVal = (List<object>)val;
            if (no >= objVal.Count)
                throw new Exception("getValue called with no: " + no.ToString() + " while we have only " + objVal.Count.ToString() + " values");
        }
        public object getValue(int no, valueType valueType = valueType.UNDEFINED)
        {
            checkNo(no);

            int indiceArray = (int)valueType;

            switch (dType[no])
            {
                case valueDTP.INT:
                    List<int?> valInt = (List<int?>) val;
                    return valInt[no];
                case valueDTP.DOUBLE:
                    List<double?> valDbl = (List<double?>)val;
                    return valDbl[no];
                case valueDTP.STRING:
                    List<string> valStr = (List<string>)val;
                    return valStr[no];
                case valueDTP.M3INT:
                    if (valueType == global::loadData.valueType.UNDEFINED)
                        throw new Exception("ValueAdd.getValue: cannot get array[UNDEFINED]");
                    List<int[]> valIntArr = (List<int[]>)val;
                    return valIntArr[no][indiceArray];
                case valueDTP.M3DOUBLE:
                    if (valueType == global::loadData.valueType.UNDEFINED)
                        throw new Exception("ValueAdd.getValue: cannot get array[UNDEFINED]");
                    List<double[]> valDblArr = (List<double[]>)val;
                    return valDblArr[no][indiceArray];
            }
            throw new Exception("I should not be there in valueVal.getValue " + no.ToString() + ", " + dType[no].ToString());
        }
        void checkNbCol(int no)
        {
            List<object> objVal = (List<object>)val;
            if (no > objVal.Count)
                throw new Exception("value.checkNBCol trying to add " + (objVal.Count - no).ToString() + " new values, it is too much... (increase the number of measure in the inverter)");
            while (objVal.Count <= no)
            {
                throw new Exception("data slot not created !!");
 //               objVal.Add(null);
            }
        }
        void checkDataTP(int no, valueDTP valDTP)
        {
            if (dType[no] != valDTP)
            {
                throw new Exception("dataType do not correspond ! " + no.ToString() + ": " + valDTP.ToString() + ", dType: " + dType[no].ToString());
            }
        }
        public void setValue(int no, int value)
        {
            checkNbCol(no);
            checkDataTP(no, valueDTP.INT);

            List<object> doubleVal = (List<object>)val;
            if (doubleVal[no] != null)
                throw new Exception("value.setValue pushing a second value in the slot! ");
            doubleVal[no] = value;
        }
        public void setValue(int no, double value)
        {
            checkNbCol(no);
            checkDataTP(no, valueDTP.DOUBLE);

            List<object> doubleVal = (List<object>)val;
            if (doubleVal[no] != null)
                throw new Exception("value.setValue pushing a second value in the slot! ");
            doubleVal[no] = value;
        }
        public void setValue(int no, string value)
        {
            checkNbCol(no);
            checkDataTP(no, valueDTP.STRING);

            List<object> doubleVal = (List<object>)val;
            if (doubleVal[no] != null)
                throw new Exception("value.setValue pushing a second value in the slot! ");
            doubleVal[no] = value;
        }
        public void setValue(int no, int[] value)
        {
            checkNbCol(no);
            checkDataTP(no, valueDTP.M3INT);

            List<object> doubleVal = (List<object>)val;
            if (doubleVal[no] != null)
                throw new Exception("value.setValue pushing a second value in the slot! ");
            doubleVal[no] = value;
        }
        public void setValue(int no, double[] value)
        {
            checkNbCol(no);
            checkDataTP(no, valueDTP.M3DOUBLE);

            List<object> doubleVal = (List<object>)val;
            if (doubleVal[no] != null)
                throw new Exception("value.setValue pushing a second value in the slot! ");
            doubleVal[no] = value;
        }
    }
    /// <summary>
    /// STRINGVAL
    /// </summary>
    public class stringVal
    {
        List<string> stringNames;
        List<valueVal> vVals;

        public stringVal()
        {
            stringNames = new List<string>();
            vVals = new List<valueVal>();
        }
        public Boolean isMeasureExist(String str, int no, valueDTP dataType)
        {
            return vVals[getStringNo(str)].isMeasureExist(no, dataType);
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
            vVals.Add(new valueVal());
            if (vVals.Count != stringNames.Count)
                throw new Exception("stringVal.getStrinNo: internal inconsistency");
            return noStr;
        }
        public void checkSlot( String str, int no, valueDTP valueDataType)
        {
            vVals[getStringNo(str)].checkSlot(no, valueDataType);
        }
        public int measureAdd(String str, valueDTP dtypeMeasure)
        {
            return vVals[getStringNo(str)].measureAdd(dtypeMeasure);
        }

        public object getValue(string str, int no, valueType valueType = valueType.MEAN)
        {
            return vVals[getStringNo(str)].getValue(no, valueType);
        }
        public void setValue(string str, int no, int value)
        {
            vVals[getStringNo(str)].setValue(no, value);
        }
        public void setValue(string str, int no, double value)
        {
            vVals[getStringNo(str)].setValue(no, value);
        }
        public void setValue(string str, int no, string value)
        {
            vVals[getStringNo(str)].setValue(no, value);
        }
        public void setValue(string str, int no, int[] value)
        {
            vVals[getStringNo(str)].setValue(no, value);
        }
        public void setValue(string str, int no, double[] value)
        {
            vVals[getStringNo(str)].setValue(no, value);
        }
    }
    /// <summary>
    /// DATEVAL
    /// </summary>
    public class dateVal
    {
        List<DateTime> aDates;
        List<stringVal> sVals;

        public dateVal()
        {
            aDates = new List<DateTime>();
            sVals = new List<stringVal>();
        }
        public Boolean isMeasureExist(DateTime dt, String str, int no, valueDTP dataType)
        {
            return sVals[getDateNo(dt)].isMeasureExist(str, no, dataType);
        }

        int getDateNo(DateTime dt)
        {
            for (int i = 0; i < aDates.Count(); i++)
            {
                if (dt == aDates[i])
                    return i;
            }
            this.aDates.Add(dt);
            sVals.Add(new stringVal());
            return getDateNo(dt);
        }
        public void checkSlot(DateTime dt, String str, int no, valueDTP valueDataType){
            sVals[getDateNo(dt)].checkSlot(str, no, valueDataType);
        }

        public int measureAdd(DateTime dt, String str, valueDTP dtypeMeasure)
        {
            return sVals[getDateNo(dt)].measureAdd(str, dtypeMeasure);
        }
        public object getValue(DateTime dt, string str, int no, valueType valueType = valueType.MEAN)
        {
            return sVals[getDateNo(dt)].getValue(str, no, valueType);
        }
        public void setValue(DateTime dt, string str, int no, int value)
        {
            sVals[getDateNo(dt)].setValue(str, no, value);
        }
        public void setValue(DateTime dt, string str, int no, double value)
        {
            sVals[getDateNo(dt)].setValue(str, no, value);
        }
        public void setValue(DateTime dt, string str, int no, string value)
        {
            sVals[getDateNo(dt)].setValue(str, no, value);
        }
        public void setValue(DateTime dt, string str, int no, int[] value)
        {
            sVals[getDateNo(dt)].setValue(str, no, value);
        }
        public void setValue(DateTime dt, string str, int no, double[] value)
        {
            sVals[getDateNo(dt)].setValue(str, no, value);
        }
    }
    /// <summary>
    /// INVERTER
    /// </summary>
    public enum invType { WEBBOX = 1, SENSOR = 2, INVERTER = 3 };
    public enum valueType { MEAN = 0, MIN = 1, MAX = 2, UNDEFINED=99 };
    public class inverter
    {
        public String name;
        public String company;
        public String model;
        public String serialNo;
        public long power;
        public Dictionary<String, int> measureInternalNameArray;
        public dateVal values;        // dateVal, StringVal, ValueVal
        public invType type;
        public int nbStrings;
        public int nbMeasures;
        public String sensorSN;

        public inverter(String company, String name, String model, String serialNo, long power = 0, invType type = invType.INVERTER,
                        int nbMeasures = -1, int nbStrings = 20, String sensorSN = null)
        {
            if (nbMeasures == -1)
                nbMeasures = glob.MAX_VALUES;
            this.name = name;
            this.model = model;
            this.serialNo = serialNo;
            this.type = type;
            this.measureInternalNameArray = new Dictionary<string, int>();     // internalNames
            if (power == 0)
            {
                string output = new string(model.Where(c => char.IsDigit(c)).ToArray());
                power = 0;
                long.TryParse(output, out power);
            }
            this.company = company;
            this.nbMeasures = nbMeasures;
            this.nbStrings = nbStrings;
            this.sensorSN = sensorSN;
            if (this.name.ToLower().Contains("webbox"))
                this.type = invType.WEBBOX;
            else
                if (this.name.ToLower().Contains("sensor"))
                    this.type = invType.SENSOR;
            this.values = new dateVal();
        }
        int getColNo(DateTime mDate, String measureName, String str)
        {
            int colNo;

            string internalName = glob.getInternalName(measureName);

            // check in the measure is mapped
            if (glob.allMNames.ContainsKey(measureName))
                internalName = glob.allMNames[measureName];

            // we need to add a new value slot for our new measure
            measureDef mDef = glob.allM[internalName];
            if (mDef == null)
                throw new Exception("measure: " + measureName + "(internal: " + internalName + ") not found in glob.allM");

            // do we already encountered this measure ?
            if(measureInternalNameArray.ContainsKey(internalName)){
                this.values.checkSlot(mDate, str, measureInternalNameArray[internalName], mDef.valueDataType);
                return measureInternalNameArray[internalName];
            }

            if(! glob.allM.ContainsKey(internalName)){
                // no matching mesure found in our catalog !!!
                throw new Exception ("inverter.getColNo: measure: " + measureName + "(" + internalName + ") not found in allMNames neither allM");
            }

            colNo =  this.values.measureAdd(mDate, str, mDef.valueDataType);
            measureInternalNameArray.Add(internalName, colNo);

            return colNo;
        }
        public object getValue(DateTime dt, string str, string measureName, valueType valueType = valueType.MEAN)
        {
            return getValue(dt, str, getColNo(dt, measureName, str), valueType);
        }
        public object getValue(DateTime dt, string str, int no, valueType valueType = valueType.MEAN)
        {
            return values.getValue(dt, str, no, valueType);
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
    }
}


/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReadWriteCsv;
using System.IO;

namespace loadCsv
{
class Program
{
static void Main(string[] args)
{
    if (args.Length == 0)
    {
        Console.WriteLine("Usage: loadCsv site_name");
        return;
    }
    loadData ld = new loadData();
    ld.run(args[0]);
}
}
class loadData
{
Dictionary<String, String> transcodName = new Dictionary<string, string>()
{
//               {"4530432", "7"},
//               {"4609792", "8"},
//               {"4529920", "9"},
    // from site nortHillFarm
        {"6627584", "-1"},//1
        {"4529920","-1"},//2
        {"2432512","-1"},//3
        {"4609792","-1"},
        {"2506496","-1"},
        {"4608512","-1"},
        {"4608256","-1"},
        {"4608000","-1"},
        {"4606464","-1"},
        {"4606208","-1"},
        {"4605952","-1"},
        {"4530432","-1"}
};

List<String[]> paramDef = new List<String[]> {
    // displayName, root, suffix1, suffix2, index in values[], Unit, neededToWriteARow, [MinMaxMean=1, MeanOnly = 0]
    new string [] {"dc_Current", "DcMs", "Amp", "", "1", "A", "1", "0"},
    new string [] {"dc_Voltage","DcMs", "Vol", "", "2", "V", "1", "1"},
    new string [] {"dc_Power", "DcMs", "Watt", "", "3", "W", "1", "1"},
    new string [] {"environmentTemp", "Env", "TmpVal", "", "4", "C", "1", "1"},
    new string [] {"envirnomentTotalInsolation", "Env", "TotInsol", "", "5", "w/m2", "1", "1"},
    new string [] {"envirnomentWindspeed", "Env", "HorWSpd", "", "6", "m/s", "1", "1"},
    new string [] {"current", "GridMs", "A", "", "7", "A", "1", "1"},
    new string [] {"frequency", "GridMs", "Hz", "", "8", "Hz", "1", "1"},
    new string [] {"voltage", "GridMs", "PhV", "", "9", "V", "1", "1"},
    new string [] {"activePower", "GridMs", "TotW", "", "10", "W", "1", "1"},
    new string [] {"mod_Temp", "Mdul", "TmpVal", "", "11", "C", "0", "1"},
    new string [] {"time_FeedIn", "Metering", "TotFeedTms", "", "12", "?", "0", "0"},
    new string [] {"time_Operating", "Metering", "TotOpTms", "", "13", "h", "0", "0"},
    new string [] {"Out_EnergyDaily", "Metering", "TotWhOut", "", "14", "W", "1", "0"},
    new string [] {"evt_Description", "Operation", "Evt", "Dsc", "15", "String", "0", "0"},
    new string [] {"evt_No", "Operation", "Evt", "EvtNo", "16", "String", "1", "0"},
    new string [] {"evt_NoShort", "Operation", "Evt", "EvtNoShrt", "17", "String","0", "0"},
    new string [] {"evt_Msg", "Operation", "Evt", "Msg", "18", "String", "1", "0"},
    new string [] {"evt_Prior", "Operation", "Evt", "Prio", "19", "String", "0", "0"},
    new string [] {"isol_Flt", "Isolation", "Flt", "", "20", "String", "0", "1"},
    new string [] {"isol_LeakRis", "Isolation", "LeakRis", "", "21", "String", "0", "1"},
    new string [] {"gridSwitchCount", "Operation", "GriSwCnt", "", "22", "String", "0", "0"},
    new string [] {"Health", "Operation", "Health", "", "23", "String", "0", "0"} };


private allMeasures allM = new allMeasures();
private int MAX_VALUES_PER_LINE = 150;
private int MAX_VALUES;
private const int MAX_STRING = 10;
dbserver dbs;
private Boolean [] bFlushMaster;

public void run(String siteName)
{
    MAX_VALUES = paramDef.Count + 4;        // 4 for date, time, inverterSN, String
    Directory.SetCurrentDirectory(siteName);
    dbs = new dbserver(siteName);
    String[] allFileNames = Directory.GetFiles(".", "*.csv", SearchOption.AllDirectories);
    Boolean BOnlyOne = false;
    bFlushMaster = new Boolean[paramDef.Count+5];
    bFlushMaster[0] = false;
    bFlushMaster[1] = false;
    bFlushMaster[2] = false;
    bFlushMaster[3] = false;
    bFlushMaster[4] = false;
    for (int i = 0; i < paramDef.Count + 5; i++)
    {
        if (i < 5)
            bFlushMaster[i] = false;
        else
            bFlushMaster[i] = paramDef[i-5][6] == "1" ? true : false;
    }
    foreach (String fileName in allFileNames)
    {
        oneMeasure oneM = loadACsv(fileName);
        if (oneM.values.Count > 0)
            allM.Add(oneM);
        if (BOnlyOne)
            break;
    }
    Directory.SetCurrentDirectory("..");

    dbs.checkInverters(siteName, allM);
    dbs.checkDateTimeLine(allM);

    processData(allM);
            
    uploadData(siteName);
}

oneMeasure loadACsv(String fileName)
{
    int LineNo = 0;
    Console.WriteLine("Loading: " + fileName);
    oneMeasure oneM = new oneMeasure(MAX_VALUES_PER_LINE);

    using (CsvFileReader reader = new CsvFileReader(fileName))
    {
        CsvRow row = new CsvRow();
        while (reader.ReadRow(row) && LineNo++ < 10000)
        {
            switch (LineNo)
            {
                case 1:
                case 2:
                case 3:
                    break;
                case 4:
                    oneM.inverter = row.ToArray();
                    break;
                case 5:
                    oneM.inverterType = row.ToArray();
                    break;
                case 6:
                    oneM.inverterSN = row.ToArray();
                    break;
                case 7:
                    row.Add("generated");
                    row.Add("perf Ratio");
                    oneM.measureName = row.ToArray();
                    break;
                case 8:
                    break;
                case 9:
                    row.Add("kWh");
                    row.Add("%");
                    oneM.measureUnit = row.ToArray();
                    break;
                default:
                    row.Add("");
                    row.Add("");
                    oneM.addMeasure(row);
                    if(oneM.env == null){
                        oneM.env = new double[2][];
                        oneM.env[0] = new double[row.Count];
                        oneM.env[1] = new double[row.Count];
                    }
                    break;
            }
            //foreach (string s in row)
            // {
            //Console.Write(s);
            //Console.Write(" ");
            //}
            //Console.WriteLine();
        }
    }
    return oneM;
}
void processData(allMeasures allM)
{
    Dictionary<String, Double> currentGenerated = new Dictionary<string, double>();
    List<oneMeasure> allMSorted = allM.OrderBy(m => m.dateMin).ToList<oneMeasure>();
    foreach (oneMeasure oneM in allMSorted )
    {


    }


}
void uploadMe(String[][] dt, String siteName)
{
    dbs.loadData(dt, bFlushMaster, siteName);
}
void uploadData(String siteName)
{
    String currentInv = "????";
    // array per Date
    //   then per String
    String[][] valueUpload = null;

    for (int iOneMIndx = 0; iOneMIndx < allM.Count(); iOneMIndx++)
    {
        oneMeasure myM = allM[iOneMIndx];

        MAX_VALUES_PER_LINE = paramDef.Count + 5;
        currentInv = "????";
        Console.WriteLine("upLoading: " + myM.values[0][0]);

        for (int iLine = 0; iLine < myM.values.Count; iLine++)
        {
            for (int iCol = 0; iCol < myM.values[iLine].Length; iCol++)
            {
                if (currentInv != myM.inverterSN[iCol])
                {
                    // Skip the first col, and the empty columns
                    if (myM.inverterSN[iCol] == null || myM.inverterSN[iCol].Length == 0)
                    {
                        currentInv = "????";
                        continue;
                    }
                    // go to our server
                    if (valueUpload != null)
                        uploadMe(valueUpload, siteName);

                    valueUpload = new String[MAX_STRING][];
                    currentInv = myM.inverterSN[iCol];

                    DateTime dtMeasure = DateTime.Parse(myM.values[iLine][0]);
                    for (int i = 0; i < MAX_STRING; i++)
                    {
                        valueUpload[i] = new String[MAX_VALUES_PER_LINE];

                        valueUpload[i][0] = dtMeasure.ToShortDateString();
                        valueUpload[i][1] = dtMeasure.ToShortTimeString();
                        valueUpload[i][2] = myM.inverterSN[iCol];
                        valueUpload[i][3] = null;
                        valueUpload[i][4] = myM.inverterType[iCol];
                    }
                }

                //Decode our Measure (should be cached...)
                String[] mName = glob.decodeName(myM.measureName[iCol]);

                // get our String in the array
                int stringNo = 0;
                if (mName[3] != "" && mName.Length != 0)
                {
                    for (int i = 1; i < MAX_STRING; i++)
                    {
                        if (valueUpload[i][3] == mName[3])
                        {
                            stringNo = i;
                            break;
                        }
                        if (valueUpload[i][3] == null)
                        {
                            valueUpload[i][3] = mName[3];
                            stringNo = i;
                            break;
                        }
                    }
                    //throw new Exception("Impossible to store the string for Measure: " + myM.measureName[iCol]);
                }

                // find measures def
                bool bMeasureFound = false;
                if (transcodName.ContainsKey(mName[0]))
                {
                    if (transcodName[mName[0]] == "-1")
                    {
                        bMeasureFound = true;
                    }
                    else
                    {
                        foreach (String[] oneDef in paramDef)
                        {
                            if (oneDef[4] == transcodName[mName[0]])
                            {
                                if (int.Parse(oneDef[4]) >= 0)
                                {
                                    if (myM.values[iLine][iCol] != null && myM.values[iLine][iCol].Length != 0)
                                    {
                                        valueUpload[stringNo][int.Parse(oneDef[4]) + 4] = myM.values[iLine][iCol];
                                    }
                                }
                                bMeasureFound = true;
                            }
                        }
                    }
                }
                else
                {
                    foreach (String[] oneDef in paramDef)
                    {
                        if (oneDef[1].ToLower() == mName[0].ToLower())
                        {
                            if (mName[1] == null || (oneDef[2].ToLower() == mName[1].ToLower()))
                            {
                                if ((oneDef[3] == "" || mName[2] == "") ||
                                    (oneDef[3].ToLower() == mName[2].ToLower()))
                                {
                                    if (int.Parse(oneDef[4]) >= 0)
                                    {
                                        if (myM.values[iLine][iCol] != null && myM.values[iLine][iCol].Length != 0)
                                        {
                                            valueUpload[stringNo][int.Parse(oneDef[4]) + 4] = myM.values[iLine][iCol];
                                        }
                                    }
                                    bMeasureFound = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!bMeasureFound)
                {
                    continue; 

                    throw new Exception("measure not used !!!");
                }

            }       // next ICol
        }
        if (valueUpload != null)
        {
            uploadMe(valueUpload, siteName);
            valueUpload = null;
        }

    }
}
}
public class allMeasures : List<oneMeasure>
{
//        List<oneMeasure> allM;
public allMeasures()
{
    //            allM = new List<oneMeasure>();
}
}
public class oneMeasure
{
private int max_values = 200;
public String siteName;
public DateTime dateMin;
public DateTime dateMax;
public String[] inverter;
public String[] inverterSN;
public String[] inverterType;
public String[] measureName;
//public String[] measureType;
public String[] measureUnit;        // can get less element if the last are null...
public double[][] env;

public List<String[]> values;

public oneMeasure(int mv = 200)
{
    max_values = mv;
    values = new List<string[]>();
    env = null;
}
public void addMeasure(CsvRow csvRow)
{
    values.Add(csvRow.ToArray<String>());
    try
    {
        DateTime dt = DateTime.Parse(csvRow[0]);
        // load dateMin/Max
        if (dateMin == null || dt < dateMin)
            dateMin = dt;
        if (dateMax == null || dt > dateMax)
            dateMax = dt;
    }
    catch(Exception ex)
    {
        ;
    }

}
public String[] getData(string inv, string measure)
{
    int indx = 0;
    Boolean bOK = false;
    foreach (String mName in inverter)
    {
        if (mName == inv)
        {
            bOK = true;
            break;
        }
        indx++;
    }
    if (!bOK)
        throw new Exception("inverter " + inv + " not found");

    String[] data = new String[max_values];

    data[0] = inverter[indx];
    data[1] = measureName[indx];
    int indexValue = 2;
    foreach (string[] vals in values)
    {
        data[indexValue] = values[indexValue - 2][indx];
        indexValue++;
    }
    return data;
}
}
}
*/
