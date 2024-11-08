using System.Collections.Generic;

using System.IO;

using System.Text;

using System.Xml;

using System.Windows.Forms;



namespace WindomXpAniTool

{

    struct script

    {

        public int unk;

        public float time;

        public string squirrel;

    }

    class animation

    {

        public string name;

        public string squirrelInit = "";

        public List<hod2v1> frames;

        public List<script> scripts;

        public void loadFromAni(ref BinaryReader br, ref hod2v0 structure)

        {

            frames = new List<hod2v1>();

            scripts = new List<script>();



            //load Name

            Encoding ShiftJis = Encoding.GetEncoding("shift_jis");

            name = ShiftJis.GetString(br.ReadBytes(256)).TrimEnd('\0');



            //Read Initial Script

            int textLength = br.ReadInt32();

            if (textLength != 0)

            {

                squirrelInit = ShiftJis.GetString(br.ReadBytes(textLength));

            }



            //Read Hod Files

            int hodCount = br.ReadInt32();

            for (int i = 0; i < hodCount; i++)

            {

                short nameLength = br.ReadInt16();

                hod2v1 nHod = new hod2v1(ShiftJis.GetString(br.ReadBytes(nameLength)));

                nHod.loadFromBinary(ref br, ref structure);

                frames.Add(nHod);

            }



            //Read Script Files

            int scriptCount = br.ReadInt32();

            for (int i = 0; i < scriptCount; i++)

            {

                script ns = new script();

                ns.unk = br.ReadInt32();

                ns.time = br.ReadSingle();

                textLength = br.ReadInt32();

                ns.squirrel = ShiftJis.GetString(br.ReadBytes(textLength));

                scripts.Add(ns);

            }

        }



        public void loadFromAniOld(ref BinaryReader br)

        {

            frames = new List<hod2v1>();

            scripts = new List<script>();



            //load Name

            Encoding ShiftJis = Encoding.GetEncoding("shift_jis");

            name = ShiftJis.GetString(br.ReadBytes(256)).TrimEnd('\0');



            //Read Hod Files

            int hodCount = br.ReadInt32();

            for (int i = 0; i < hodCount; i++)

            {

                hod1 nHod = new hod1(ShiftJis.GetString(br.ReadBytes(30)).TrimEnd('\0'));

                nHod.loadFromBinary(ref br);

                frames.Add(nHod.convertToHod2v1());

            }



            //Read Script Files

            int scriptCount = br.ReadInt32();

            for (int i = 0; i < scriptCount; i++)

            {

                script ns = new script();

                ns.unk = br.ReadInt32();

                ns.time = br.ReadSingle();

                int textLength = br.ReadInt32();

                ns.squirrel = ShiftJis.GetString(br.ReadBytes(textLength));

                scripts.Add(ns);

            }

        }

        public void saveToAni(ref BinaryWriter bw)

        {

            Encoding ShiftJis = Encoding.GetEncoding("shift_jis");

            byte[] shiftjistext = ShiftJis.GetBytes(name);

            bw.Write(shiftjistext);

            bw.BaseStream.Seek(256 - shiftjistext.Length, SeekOrigin.Current);

            shiftjistext = ShiftJis.GetBytes(squirrelInit);

            bw.Write(shiftjistext.Length);

            if (shiftjistext.Length > 0)

                bw.Write(shiftjistext);

            bw.Write(frames.Count);



            for (int i = 0; i < frames.Count; i++)

            {

                shiftjistext = ShiftJis.GetBytes(frames[i].filename);

                bw.Write((short)shiftjistext.Length);

                bw.Write(shiftjistext);

                frames[i].SaveToBinary(ref bw);

            }



            bw.Write(scripts.Count);

            for (int i = 0; i < scripts.Count; i++)

            {

                bw.Write(scripts[i].unk);

                bw.Write(scripts[i].time);

                shiftjistext = ShiftJis.GetBytes(scripts[i].squirrel);

                bw.Write(shiftjistext.Length);

                if (shiftjistext.Length > 0)

                    bw.Write(shiftjistext);

            }

        }

        public void extractToFolderXML(string folder, int hodEType)

        {

            DirectoryInfo di;

            try

            {

                di = Directory.CreateDirectory(folder);



                //save hod files

                //for (int i = 0; i < frames.Count; i++)

                //{ 

                string extension = "";

                if (hodEType == 1)

                    extension = ".xml";



                //    if (!File.Exists(Path.Combine(di.FullName, frames[i].filename + extension)))

                //    {

                //        BinaryWriter bw = new BinaryWriter(File.Open(Path.Combine(di.FullName, frames[i].filename), FileMode.CreateNew));

                //        //bw.Write(frames[i].data);

                //        bw.Close();

                //    }

                // }



                XmlWriterSettings xws = new XmlWriterSettings();

                xws.Indent = true;



                using (XmlWriter xw = XmlWriter.Create(Path.Combine(di.FullName, "script.xml"), xws))

                {

                    xw.WriteStartDocument();

                    xw.WriteStartElement("Script");

                    xw.WriteAttributeString("name", name);

                    xw.WriteElementString("Squirrel_Init", squirrelInit);



                    xw.WriteStartElement("Hod_List");

                    for (int i = 0; i < frames.Count; i++)

                    {

                        string index = i.ToString("D2");

                        xw.WriteElementString("Hod", frames[i].filename + extension);

                        frames[i].SaveToFile(folder, hodEType);

                    }

                    xw.WriteEndElement();



                    xw.WriteStartElement("Squirrel_Scripts");

                    for (int i = 0; i < scripts.Count; i++)

                    {

                        xw.WriteStartElement("Script");

                        xw.WriteAttributeString("unk", scripts[i].unk.ToString());

                        xw.WriteAttributeString("time", scripts[i].time.ToString());

                        xw.WriteString(scripts[i].squirrel.ToString());

                        xw.WriteEndElement();

                    }

                    xw.WriteEndElement();

                    xw.WriteEndElement();

                    xw.WriteEndDocument();

                }



                // 正常に処理が完了した場合にメッセージボックスを表示

                //MessageBox.Show("XMLの展開が正常に完了しました。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }

            catch (System.Exception ex)

            {

                // エラーが発生した場合にメッセージボックスを表示

                MessageBox.Show("エラーが発生しました: " + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        public void injectFromFolderXML(string folder, ref hod2v0 structure)

        {

            XmlDocument doc = new XmlDocument();

            doc.Load(Path.Combine(folder, "script.xml"));

            XmlNode mainNode = doc.SelectSingleNode("Script");

            bool nameChanged = false;

            string prevName = name;

            if (name != mainNode.Attributes["name"].Value)

                nameChanged = true;

            name = mainNode.Attributes["name"].Value;



            XmlNode squirrelInitNode = mainNode.SelectSingleNode("Squirrel_Init");

            squirrelInit = squirrelInitNode.InnerText;



            XmlNode hodNode = mainNode.SelectSingleNode("Hod_List");

            

            // ここで hodNode が null でないことを確認

            if (hodNode == null)

            {

                MessageBox.Show("Hod_List が XML に存在しません", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return; // エラーが発生した場合は処理を中断

            }



            XmlNodeList hodlist = hodNode.ChildNodes;

            frames.Clear();
            //string parentFolderPath = Directory.GetParent(folder).FullName;
            for (int i = 0; i < hodlist.Count; i++)

            {
                int folderCount = 0;
                hod2v1 nFrame = new hod2v1(hodlist.Item(i).InnerText);
                string hodfolder = Path.Combine(folder, i.ToString("D2"));
                nFrame.loadFromFile(Path.Combine(hodfolder, nFrame.filename), ref structure);
                folderCount = folderCount + 1;
                frames.Add(nFrame);

            }



            XmlNode eventScripts = mainNode.SelectSingleNode("Squirrel_Scripts");

            XmlNodeList eventList = eventScripts.ChildNodes;

            scripts.Clear();

            for (int i = 0; i < eventList.Count; i++)

            {

                XmlNode eventItem = eventList.Item(i);

                script nScript = new script();

                int pInt;

                if (int.TryParse(eventItem.Attributes["unk"].Value, out pInt))

                    nScript.unk = pInt;



                float pFloat;

                if (float.TryParse(eventItem.Attributes["time"].Value, out pFloat))

                    nScript.time = pFloat;



                nScript.squirrel = eventItem.InnerText;



                scripts.Add(nScript);

            }



            if (nameChanged)

            {

                try

                {

                    string newFolder = folder.Replace(helper.replaceUnsupportedChar(prevName), helper.replaceUnsupportedChar(name));

                    Directory.Move(folder, newFolder);

                }

                catch

                {

                    name = prevName;

                }

            }



            // 正常に処理が完了した場合にメッセージボックスを表示

            //MessageBox.Show("取り込みが正常に完了しました。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        public void extractToFolderTXT(string folder, int hodEType)

        {

            DirectoryInfo di;
            try{

            

                di = Directory.CreateDirectory(folder);



                string extension = "";

                switch (hodEType)

                {

                    case 1:

                        extension = ".xml";

                        break;

                    case 2:

                        extension = ".txt";

                        break;

                }

                //save hod files

                //for (int i = 0; i < frames.Count; i++)

                //{

                //    if (!File.Exists(Path.Combine(di.FullName, frames[i].filename)))

                //    {

                //        BinaryWriter bw = new BinaryWriter(File.Open(Path.Combine(di.FullName, frames[i].filename), FileMode.CreateNew));

                //        //bw.Write(frames[i].data);

                //        bw.Close();

                //    }

                //}



                StreamWriter sw = new StreamWriter(Path.Combine(di.FullName, "script.txt"));

                sw.WriteLine(name);

                sw.WriteLine("Squirrel_Init");

                if (squirrelInit.Length > 0)

                    sw.WriteLine(squirrelInit);

                sw.WriteLine("Hod_LIST");

                for (int i = 0; i < frames.Count; i++)

                {

                    sw.WriteLine(frames[i].filename + extension);

                    frames[i].SaveToFile(folder, hodEType);

                }

                for (int i = 0; i < scripts.Count; i++)

                {

                    sw.WriteLine("Squirrel_Script," + scripts[i].unk.ToString() + "," + scripts[i].time.ToString());

                    sw.WriteLine(scripts[i].squirrel);

                }
                    //MessageBox.Show("TXTの展開が正常に完了しました。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    sw.Close();

            }

            catch (System.Exception ex)

            {

                // エラーが発生した場合にメッセージボックスを表示

                MessageBox.Show("エラーが発生しました: " + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        public void injectFromFolderTXT(string folder, ref hod2v0 structure)

        {
          try
           {
                StreamReader sr = new StreamReader(Path.Combine(folder, "script.txt"));

                int mode = 0; //0 = name, 1 = squirrel init,2 = hodlist,3 = squirrel

                frames.Clear();

                scripts.Clear();

                hod2v1 nFrame;

                script nScript = new script();

                string prevName = name;



                while (sr.EndOfStream != true)

                {

                    bool readSwitch = true;

                    string line = sr.ReadLine();

                    if (line.Contains("Squirrel_Init"))

                    { if (mode == 3) scripts.Add(nScript); mode = 1; squirrelInit = ""; readSwitch = false; }

                    else if (line.Contains("Hod_LIST"))

                    { if (mode == 3) scripts.Add(nScript); mode = 2; readSwitch = false; }

                    else if (line.Contains("Squirrel_Script"))

                    {

                        if (mode == 3) scripts.Add(nScript);

                        mode = 3;

                        nScript = new script();

                        string[] split = line.Split(",".ToCharArray());

                        int pInt;

                        if (int.TryParse(split[1], out pInt))

                            nScript.unk = pInt;



                        float pFloat;

                        if (float.TryParse(split[2], out pFloat))

                            nScript.time = pFloat;



                        readSwitch = false;

                    }

                    if (readSwitch)

                    {

                        switch (mode)

                        {

                            case 0:

                                if (line.Replace(" ", "") != "")

                                {

                                    name = line;

                                }

                                break;

                            case 1:

                                squirrelInit += line + "\n";

                                break;

                            case 2:

                                nFrame = new hod2v1(line);

                                nFrame.loadFromFile(Path.Combine(folder, nFrame.filename), ref structure);

                                frames.Add(nFrame);

                                break;

                            case 3:

                                nScript.squirrel += line + "\n";

                                break;

                        }

                    }

                }

                if (mode == 3) scripts.Add(nScript);

                sr.Close();



                if (prevName != name)

                {

                    try

                    {

                        string newFolder = folder.Replace(helper.replaceUnsupportedChar(prevName), helper.replaceUnsupportedChar(name));

                        Directory.Move(folder, newFolder);

                    }

                    catch

                    {

                        name = prevName;

                    }

                }
                //MessageBox.Show("TXTの取り込みに成功しました。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        
            }
            catch (System.Exception ex)
            {
           // エラーが発生した場合にメッセージボックスを表示
           MessageBox.Show("エラーが発生しました: " + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        

    }

}