

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace model
{
    class SAIF_node
    {
        public List<SAIF_node> nodes;
        public string name;
        public string data;
        public SAIF_node()
        {
            name = "";
            data = "";
            nodes = new List<SAIF_node>();
        }

        public SAIF_node find_name(string _name)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].name == _name)
                    return nodes[i];
            }
            return null;
        }
        public SAIF_node find_data(string _name)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].data.Replace(" ","") == _name.Replace(" ","") )
                    return nodes[i];
            }
            return null;
        }
    }
    public class Net
    {
        public static int duratin=1;
        public string name;
        public double T0;
        public double T1;
        public double TX;
        public double TC;
        public double IG;
        public double toggle_rate
        {
            get
            {
                return TC / duratin;
            }
        }
        public double static_probability 
        {
            get
            {
                return T1 / (T1 + T0 + TX);
            }
        }

    }
    public class Module
    {
        public string name;
        public string type;
        public List<Net> netlist;
        public List<Module> nodes;
        public string[] path;
        public Module()
        {
            netlist = new List<Net>();
            nodes = new List<Module>();
        }
        public Module find_name(string _name)
        {
            _name = _name.Replace(" ", "").Replace("\\", "").ToLower();
            for (int i = 0; i < nodes.Count; i++)
            {
                string name = nodes[i].name.Replace(" ", "").Replace("\\", "").ToLower();
                if (name == _name)
                    return nodes[i];
            }
            return null;
        }
        public double Sp_avg
        {
            get
            {
                double ret = 0;
                for (int i = 0; i < netlist.Count; i++)
                {
                    ret += netlist[i].static_probability;
                }
                return (netlist.Count == 0) ? 0 : (ret / netlist.Count);
            }
        }

    }


    class Program
    {
        public class gate
        {
            public int num;
            public string name;
            public string in1;
            public string in2;
            public string output;
            public List<Transistor> trns_list;
            public gate()
            {
                num = 0;
                trns_list = new List<Transistor>();
            }
           
        }
        public class Transistor
        {
            public string name;
            public string in1;
            public string output;
            public string L;
            public string W;
            public string type;
        }
        static public SAIF_node rec_read_saif(string[] saif_str_split, ref int index)
        {

            if (saif_str_split[index][0] == '(')
            {
                SAIF_node ret =  new SAIF_node();
                ret.name = saif_str_split[index].Substring(1);

                index++;
                while (true)
                {
                    if (saif_str_split[index].IndexOfAny(new char[] {'(',')'}) == -1)
                    {
                        ret.data += saif_str_split[index] + " ";
                        index++;
                    }
                    else if(saif_str_split[index].IndexOfAny(new char[] {')'}) != -1)
                    {
                        ret.data += saif_str_split[index].Substring(0, saif_str_split[index].Length - 1) + " ";
                        index++;
                        return ret;
                    }
                    else if (saif_str_split[index].IndexOfAny(new char[] { '(' }) != -1)
                    {
                        break;
                    }
                }

                while (true)
                {
                    if (saif_str_split[index].IndexOfAny(new char[] { '(' }) != -1)
                    {
                        ret.nodes.Add(rec_read_saif(saif_str_split, ref index));
                    }
                    else if (saif_str_split[index].IndexOfAny(new char[] { ')' }) != -1)
                    {
                        index++;
                        return ret;
                    }
                }
            }
            return null;
        }
        static Module rec_fill_module(SAIF_node ins)
        {
            Module m = new Module();
            m.name = ins.data;

            for (int i = 0; i < ins.nodes.Count; i++)
            {
                if (ins.nodes[i].name == "NET")
                {
                    for (int j = 0; j < ins.nodes[i].nodes.Count; j++)
                    {
                        Net a = new Net();

                        a.name = ins.nodes[i].nodes[j].name;
                        a.T0 = Convert.ToInt32(ins.nodes[i].nodes[j].nodes[0].data);
                        a.T1 = Convert.ToInt32(ins.nodes[i].nodes[j].nodes[1].data);
                        a.TX = Convert.ToInt32(ins.nodes[i].nodes[j].nodes[2].data);
                        a.TC = Convert.ToInt32(ins.nodes[i].nodes[j].nodes[3].data);
                        a.IG = Convert.ToInt32(ins.nodes[i].nodes[j].nodes[4].data);
                        m.netlist.Add(a);
                        
                    }
                }
                else if (ins.nodes[i].name == "INSTANCE")
                {
                    m.nodes.Add(rec_fill_module(ins.nodes[i]));
                }
            }
            return m;
        }
        public struct Path_Module
        {
            public string[] path;
            public string type;
        }

        public static double year;
        public static double num_cycle; //cycles of stress and recovery
        public static string test = "solomon";
        public static string topmodule = "RS_dec";
        public static string test_top = "RS_dec_tb";
        public static string module_uut = "DUT";

        static void Main(string[] args)
        {
            //just to test!! I can remove this function and assign year somewhere else, then mymodel will go back to main()
            year = 0;
            num_cycle = year * 365 * 24 * 3600 * 1E8 + 1;
            mymodel(args);
            year = 0.5;
            num_cycle = year * 365 * 24 * 3600 * 1E8 + 1;
            mymodel(args);
            year = 1;
            num_cycle = year * 365 * 24 * 3600 * 1E8 + 1;
            mymodel(args);
            year = 5;
            num_cycle = year * 365 * 24 * 3600 * 1E8 + 1;
            mymodel(args);
        }

        static void mymodel(string[] args)
        {
            List<Path_Module> paths = new List<Path_Module>(); 
            
            System.IO.StreamWriter final = new System.IO.StreamWriter(test + "_" + year.ToString() + "_result.txt");

            System.IO.StreamReader synfile = new System.IO.StreamReader(@"inputs/" + test + "_syn.v");
            System.IO.StreamReader repfile = new System.IO.StreamReader(@"inputs/" + test + "-cell.rpt");
            System.IO.StreamReader filelib = new System.IO.StreamReader(@"inputs/" + test + ".saif");
            
            string input_syn = synfile.ReadToEnd();
            int counter = 0;
            string inreport = repfile.ReadLine();
            string m2 = "";
            int found = 0;
            int size = 0;
            int cnt = 0;
            string match = "";
            //I sould throw away the first lines of the report file
            //the while loop read one line at a time -from the report file, the verilog file has been read to the end
            while (inreport != null)
            {
                if (inreport.Length==0 || inreport[0] == ' ')
                    inreport = repfile.ReadLine();

                if (inreport == null)
                    break;
                string[] m1 = inreport.Split(new char[] { ' ', '/', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                //this for removes the numbers of each line which are not necessary for our purpose
                for (int i = 0; i < m1.Length; i++)
                {
                    if (m1[i][0] <= '9' && m1[i][0] >= '0')
                    {
                        int found1 = inreport.IndexOf(m1[i]);
                        inreport = inreport.Substring(0, found1 - 1);
                        m1 = inreport.Split(new char[] { ' ', '/', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    }
                }
                //if only one instance is in the line, we need to check the top module and put it into the output
                // else we need to read the rest too.
                if (match == "")
                {
                    if (m1.Length != 1)
                    {
                        found = input_syn.IndexOf(m1[counter]+" ");
                        counter++;

                        cnt = found - 2;

                        while (input_syn[cnt] != ' ')
                        {
                            cnt--;
                            size++;
                        }
                        //finding the new type of module from the verilog file 
                        m2 = input_syn.Substring(cnt + 1, size);
                    }
                    else
                    {
                        //this topmodule name will be named from reading the input file, I should change it manually
                        
                        m2 = topmodule;
                    }
                }
                found = input_syn.IndexOf("module " + m2 + " ");

                found = input_syn.IndexOf(m1[counter]+ " ", found);
               
                if (input_syn[found - 1] == ' ') // still we have some modules left we need to check into syn file
                {
                    size = 0;
                    cnt = found - 2;

                    while (input_syn[cnt] != ' ')
                    {
                        cnt--;
                        size++;
                    }
                    string newone = "";

                    newone = input_syn.Substring(cnt + 1, size);
                    match = newone;
                }
                else if (input_syn[found - 1] == '\\') // just because i need to read one additional line
                {
                    size = 0;
                    cnt = found - 3;

                    while (input_syn[cnt] != ' ')
                    {
                        cnt--;
                        size++;
                    }
                    string newone = "";
                    newone = input_syn.Substring(cnt + 1, size);
                    match = newone;
                }
                if (m1.Length == counter + 1) //it means its the end of line and we found our match
                {
                    for (int j = 0; j < m1.Length; j++)
                    {
                        //outfile.Write(m1[j]+"/");
                    }
                    
                    //outfile.Write("     "+match + "\n");
                    Path_Module mm = new Path_Module();
                    mm.path = m1;
                    mm.type = match;
                    paths.Add(mm);

                    inreport = repfile.ReadLine();
                    m2 = "";
                    found = 0;
                    size = 0;
                    cnt = 0;
                    counter = 0;
                    match = "";
                    if (inreport == null)
                        break;
                    //go take newone as match to the library
                }
                else
                {
                    m2 = match;
                    counter++;
                }
            }
            //************************************************************************************************************
            //********************************we get the switching activity from saif file********************************
            //************************************************************************************************************
            string saif_str = filelib.ReadToEnd();

            string[] saif_str_split = saif_str.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            

            int index = 0;
            SAIF_node root = rec_read_saif(saif_str_split, ref index);
            //in the saif file I need to give the values of the test bench and module under test manually
            SAIF_node ins = root.find_data(test_top);
            ins = ins.find_data(module_uut);
            Module top_module = rec_fill_module(ins);
            List<Module> leaves = new List<Module>();
            for (int i = 0; i < paths.Count; i++)
            {
                Module m = top_module;
                for (int j = 0; j < paths[i].path.Length; j++)
                {
                    m = m.find_name(paths[i].path[j]);
                    if (m == null)
                        break;
                }
                if (m != null)
                {
                    m.path = paths[i].path;
                    m.type = paths[i].type;
                    leaves.Add(m);
                }
            }
            //************************************************************************************************************
            //********************************we get the transistor info here from spice files****************************
            //************************************************************************************************************
            
            List<gate> gatelist = new List<gate>();
            
            System.IO.StreamReader files_trans_info = new System.IO.StreamReader("cells.sp"); //spice file
            gatelist = readlib(files_trans_info,gatelist);

            double temp = 298.15;
            double vdd = 1.1;
            double vgs = 1.1;
            double vth = 0.2;

            double leakage_power = 0;
            for (int i = 0; i < leaves.Count; i++)
            {
                leakage_power = 0;
                gate mygate = find_in_gatelist(leaves[i].type, gatelist);
                
                for (int j = 0; j < mygate.trns_list.Count; j++)
			    {
                    vth =  calc_vth(mygate.trns_list[j], vgs, leaves[i].Sp_avg);
                    double W = Convert.ToDouble(mygate.trns_list[j].W.Substring(0,mygate.trns_list[j].W.Length-1));
                    double L = Convert.ToDouble(mygate.trns_list[j].L.Substring(0,mygate.trns_list[j].L.Length-1));
                    bool nmos = (mygate.trns_list[j].type[0]=='n');
                    leakage_power += calculate_leakage(L, W , vth, temp, vdd, nmos);
			    }
                //for (int count = 0; count < leaves[i].path.Length; count++)
                //{
                //    final.Write(leaves[i].path[count] + "/");
                //}
                final.WriteLine(leakage_power);
            }
            final.Close();
            filelib.Close();
        }
//**********************************************************************************************
        static List<gate> readlib(System.IO.StreamReader filelib, List<gate> gatelist)
        {
            string input_lib = filelib.ReadToEnd();
            string []lines=input_lib.Split(new char[] { '\n'}, StringSplitOptions.RemoveEmptyEntries);
            int counter=0;
            int length = lines.Length;
            string[] splited_line={""};
            gate newgate =null;
            
            while (length !=0)
            {
                splited_line = lines[counter].Split(new char[] { ' ','\r' }, StringSplitOptions.RemoveEmptyEntries);
                if ((splited_line[0].ToUpper() == ".SUBCKT") || (splited_line[0].ToUpper()[0] == 'M'))
                {
                    if (splited_line[0].ToUpper() == ".SUBCKT")
                    {
                        //gate's info, so it uses the gate lists
                        newgate = new gate();
                        if (splited_line.Length > 4)
                        {
                            newgate.name = splited_line[1];
                            newgate.in1 = splited_line[2];
                            newgate.in2 = splited_line[3];
                            newgate.output = splited_line[4];
                        }
                        else
                            newgate.name = splited_line[1];
                    }
                    else if (splited_line[0].ToUpper() [0]== 'M')
                    {
                        //transistor's info, so it uses the transistor lists
                        Transistor tr1 = new Transistor();
                        tr1.name = splited_line[0];
                        tr1.L = splited_line[7].Substring(2);
                        tr1.W = splited_line[6].Substring(2);
                        tr1.type = splited_line[5];
                        newgate.trns_list.Add(tr1);
                        newgate.num++;
                        
                    }
                    //next line
                    counter++;
                    length--;
                    splited_line = lines[counter].Split(new char[] { ' ', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    if (splited_line[0].ToUpper()[0] == 'M')
                    {
                        Transistor tr1 = new Transistor();
                        tr1.name = splited_line[0];
                        tr1.L = splited_line[7].Substring(2);
                        tr1.W = splited_line[6].Substring(2);
                        tr1.type = splited_line[5];
                        newgate.trns_list.Add(tr1);
                        newgate.num++;
                        counter++;
                        length--;
                    }
                  
                    else if (splited_line[0].ToUpper() == ".ENDS")
                    {
                        gatelist.Add(newgate); counter++;
                        length--;
                    }
                    else
                    {
                        counter++;
                        length--;
                    }
                }
                else if (splited_line[0].ToUpper() == ".ENDS")
                {
                    gatelist.Add(newgate);
                    counter++;
                    length--;
                }
                else
                {
                    counter++;
                    length--;
                }
            }
            filelib.Close();
            return gatelist;
        }
//**********************************************************************************************
        static string find_in_X(string [] splited_line)
        {
            for (int i = 1; i < splited_line.Length; i++)
            {
                if (splited_line[i][0] > 'A' && splited_line[i][0] < 'Z')
                    return splited_line[i];
            }
            return null;
        }
//**********************************************************************************************
        static gate find_in_gatelist(string name,List<gate> gatelist)
        {
            for (int i = 0; i <gatelist.Count ; i++)
            {
                if (gatelist[i].name == name)
                    return gatelist[i];
            }
            return null;
        }
//**********************************************************************************************
        static double calc_vth(Transistor trn, double vgs, double _beta)
        {

            //stress level
            double delta_vth = 0;
            double vth = 0.20, vds = 1.1, Beta = _beta, alpha = 1.3, E0 = 0.20, Ea = 0.13; 
            double k = 8.61E-5;
            double T = 318.15;
            double Tox = 1.85;//nm
            double epsilonox = 2.81E-11;//F/m
            double A = 1.8;
            double W = Convert.ToDouble(trn.W.Substring(0,trn.W.Length-1));//um
            double L = Convert.ToDouble(trn.L.Substring(0,trn.L.Length-1));//um

            double Eox = (vgs - vth) / Tox;//V/nm
            double Cox = epsilonox * W * L * 1E+8 / Tox ;//F /////////problem?!
            double eq2 = Cox * Math.Abs(vgs - vth);
            double small_delta_v = (5.0); // mv
            double frequency = 100E6;//100 Mhz
            double kv = A * Tox * Math.Pow(eq2, 0.5) * Math.Exp((Eox / E0)) * (1 - vds / (alpha * (vgs - vth)) * Math.Exp((-1 * Ea / (k * T))));
            double clock_period = 1 / frequency;//  T in formula
            double etta = 0.35;
            double intemp1 = Math.Pow((etta * (1 - Beta) / num_cycle), 0.5);
            double temp1 = Math.Pow(1 - intemp1, 2 * num_cycle);
            double intemp2 = Math.Pow((etta * (1 - Beta) / num_cycle), 0.5);
            double temp2 =Math.Pow(1 - intemp2, 2);
            double eq3 = (1 -temp1) / (1 - temp2);
            
            delta_vth = kv * Math.Pow(Beta, 0.25) * Math.Pow(clock_period, 0.25) * Math.Pow(eq3, 0.5) + small_delta_v;
            
            vth += delta_vth / 1000.0;
            return vth;
        }
//**********************************************************************************************
        static double calculate_leakage(double leff, double weff, double vth, double temp, double vdd, bool nmos)
        {
            double U0 = (nmos) ? 3.1 * 0.036396 : 0.036396;  //mobility effect
            double M_ = 1.3;// Body effect coefficient 
            double Vg = 0;// gate voltage gate 
            double Vds = vdd; //Voltage drain source 
            double K = 8.61E-5;//Boltzman constant 
            double Tox = 1.85E-9;//m
            double epsilonox = 2.81E-11;//F/m
            double cox = epsilonox / Tox;//F
            double leakage, param1, param2, param3, param4;
            param1 = vdd * U0 * cox * (weff / leff);
            param2 = (M_ - 1.0) * Math.Pow(K * temp, 2.0);
            param3 = Math.Exp(((Vg - vth)) / (M_ * K * temp));
            param4 = 1 - Math.Exp(( -1 * vdd) / (K * temp));
            leakage = param1 * param2 * param3 * param4;
            return leakage;
        }
    }
}


