
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }
        //创建 1个客户端套接字 和1个负责监听服务端请求的线程  
        Socket socketClient = null;
        Thread threadClient = null;

        private string CheckSum(string sendmsg)
        {
            UInt32 u8Index;
            byte u8ChckSum = 0;
            byte[] arraycalc = HexStrTobyte(sendmsg);
            for (u8Index = 0; u8Index < arraycalc.Length; u8Index++)
            {
                u8ChckSum = (byte)(u8ChckSum ^ arraycalc[u8Index]);
            }
            Convert.ToString(u8ChckSum, 16);
            return Convert.ToString(u8ChckSum, 16);
        }
        private string u8TransTime(string time)
        {
            byte u8Temp = 16;
            if (u8Temp < Convert.ToByte(time))
            {
                return Convert.ToString(Convert.ToByte(time), 16);
            }
            else
            {
                return "0" + Convert.ToString(Convert.ToByte(time), 16);
            }
        }
        private string u16TransTime(string time)
        {
            UInt16 u8Temp1 = 255;
            UInt16 u8Temp2 = 4095;

            if (u8Temp1 > Convert.ToUInt16(time))
            {
                return "00" + Convert.ToString(Convert.ToUInt16(time), 16);
            }
            else if (u8Temp2 > Convert.ToUInt16(time))
            {
                return "0" + Convert.ToString(Convert.ToUInt16(time), 16);
            }
            else
            {
                return Convert.ToString(Convert.ToUInt16(time), 16);
            }

        }
        int timeout = 0;
        /// <summary>
        /// 连接服务端事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        /// <summary>
        /// 接收服务端发来信息的方法
        /// </summary>
        private void RecMsg()
        {
            while (true) //持续监听服务端发来的消息
            {
                try
                {

                    //定义一个1M的内存缓冲区 用于临时性存储接收到的信息
                    byte[] arrRecMsg = new byte[1024 * 1024];
                    //将客户端套接字接收到的数据存入内存缓冲区, 并获取其长度
                    int length = socketClient.Receive(arrRecMsg);
                    //将套接字获取到的字节数组转换为人可以看懂的字符串
                    string strRecMsg = Encoding.UTF8.GetString(arrRecMsg, 0, length);
                    //将发送的信息追加到聊天内容文本框中
                    txtClientRecMsg.AppendText("服务端 " + GetCurrentTime() + "\r\n" + strRecMsg + "\r\n");
                    if (0x33 == arrRecMsg[7])
                    {
                        if (0x35 == arrRecMsg[13])
                        {
                            MessageBox.Show(this, "预约充电设置繁忙");
                        }
                        else if (0x31 == arrRecMsg[13])
                        {
                            MessageBox.Show(this, "预约充电设置成功");
                        }
                        else if (0x32 == arrRecMsg[13])
                        {
                            MessageBox.Show(this, "预约充电设置失败");
                        }
                        btSoc.Enabled = true;
                        btSubSet.Enabled = true;
                        btDiscount.Enabled = true;
                        timeout = 0;
                        timer1.Stop();
                    }
                    else if (0x34 == arrRecMsg[7])
                    {
                        if (0x35 == arrRecMsg[13])
                        {
                            MessageBox.Show(this, "预约出行设置繁忙");
                        }
                        else if (0x31 == arrRecMsg[13])
                        {
                            MessageBox.Show(this, "预约出行设置成功");
                        }
                        else if (0x32 == arrRecMsg[13])
                        {
                            MessageBox.Show(this, "预约出行设置失败");
                        }
                        btDep.Enabled = true;
                        timeout = 0;
                        timer1.Stop();
                    }
                    else if (0x36 == arrRecMsg[7])
                    {
                        GetChrgSyncData(arrRecMsg);
                    }
                    else if (0x37 == arrRecMsg[7])
                    {
                        GetDepSyncData(arrRecMsg);
                    }
                    else
                    {

                    }

                }
                catch (Exception ex)
                {
                    this.txtMsg.AppendText("远程服务器已中断连接！" + "\r\n");
                    this.btnListenServer.Enabled = true;
                    break;
                }
            }
        }
        private static byte[] HexStrTobyte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2).Trim(), 16);
            return returnBytes;
        }

        /// <summary>
        /// 发送字符串信息到服务端的方法
        /// </summary>
        /// <param name="sendMsg">发送的字符串信息</param>
        private void ClientSendMsg(string sendMsg, byte msgsendtype)
        {
            try
            {
                //将输入的内容字符串转换为机器可以识别的字节数组
                byte[] arrClientSendMsg = Encoding.UTF8.GetBytes(sendMsg);//UTGetBytes(sendMsg);

                socketClient.Send(arrClientSendMsg);
                //将发送的信息追加到聊天内容文本框中
            }
            catch (Exception ex)
            {
                this.txtMsg.AppendText("远程服务器已中断连接,无法发送消息！" + "\r\n");
            }
        }
        /// <summary>
        /// 获取当前系统时间的方法
        /// </summary>
        /// <returns>当前时间</returns>
        private DateTime GetCurrentTime()
        {
            DateTime currentTime = new DateTime();
            currentTime = DateTime.Now;
            return currentTime;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void startSocketClient()
        {
            //定义一个套字节监听  包含3个参数(IP4寻址协议,流式连接,TCP协议)
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //需要获取文本框中的IP地址
            txtIP.Text = "192.168.225.1";
            txtPort.Text = "28888";
            IPAddress ipaddress = IPAddress.Parse(this.txtIP.Text.Trim());
            //将获取的ip地址和端口号绑定到网络节点endpoint上
            IPEndPoint endpoint = new IPEndPoint(ipaddress, int.Parse(this.txtPort.Text.Trim()));
            //这里客户端套接字连接到网络节点(服务端)用的方法是Connect 而不是Bind
            try
            {
                socketClient.Connect(endpoint);
                //socketHearBreak.Connect(endpoint);
                this.txtMsg.AppendText("客户端连接服务端成功！" + "\r\n");
                //创建一个线程 用于监听服务端发来的消息
                threadClient = new Thread(RecMsg);
                //将窗体线程设置为与后台同步
                threadClient.IsBackground = true;
                //启动线程
                threadClient.Start();

                HearBreak.Start();
            }
            catch (Exception ex)
            {
                this.txtMsg.AppendText("远程服务端断开，连接失败！" + "\r\n");
                this.btnListenServer.Enabled = true;
            }
        }
        bool bSwitch = false;
        private void btnListenServer_Click_1(object sender, EventArgs e)
        {
            Ping pp = new Ping();
            PingReply ppResult = pp.Send("192.168.225.1", 100);
            if (true == bSwitch)
            {
                socketClient.Close();
                btnListenServer.Text = "Connect";
                bSwitch = false;
                return;
            }
            if (IPStatus.Success == ppResult.Status)
            {
                startSocketClient();
                btnListenServer.Text = "Unconnect";
                bSwitch = true;
            }
            else
            {
                MessageBox.Show(this, "当前网络繁忙");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Stop the timer
            HearBreak.Stop();
            string msg = null;
            msg = "0D052A7000170000000000000000000000000000000000000000000000670D0A";
            ClientSendMsg(msg, 2);
            HearBreak.Start();
        }

        private void targetSoc_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.targetSoc.Text = targetSoc.Items.ToString();
        }

        private void immeset_Click(object sender, EventArgs e)
        {
            timer1.Start();
            string immechrgtime = null;
            btSoc.Enabled = false;
            if (0 == Convert.ToByte(targetSoc.Text))
            {
                MessageBox.Show(this, "请选择目标SOC","Information cue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            startSocketClient();
            
            immechrgtime = "C2002401" + Convert.ToString(Convert.ToByte(targetSoc.Text.Trim()), 16) + "00000000000000000000000000000000000000000000000000000000000000000000";
            immechrgtime = "0D052A" + immechrgtime + CheckSum(immechrgtime) + "0D0A";

            ClientSendMsg(immechrgtime,2);
            
            //System.Threading.Thread.Sleep(5000);
            //socketClient.Close();


        }

        private void btSubSet_Click(object sender, EventArgs e)
        {
            timer1.Start();
            int u8dayofWeek = 0;
            byte u8Temp = 16;
            string subchrgtime = null;

            btSubSet.Enabled = false;
            for (byte loop = 0; loop < 7; loop++)
            {
                if (true == ChrgWeekEnable.GetItemChecked(loop))
                {
                    u8dayofWeek |= (1 << loop);
                }
            }

            subchrgtime += "C2002401000100000000";

            if (u8Temp < u8dayofWeek)
            {
                subchrgtime += Convert.ToString(u8dayofWeek, 16);
            }
            else
            {
                subchrgtime += "0" + Convert.ToString(u8dayofWeek, 16);
            }

            subchrgtime += u8TransTime(SubMonMin.Text.Trim()) + u8TransTime(SubMonHour.Text.Trim());
            subchrgtime += u8TransTime(SubTuesMin.Text.Trim()) + u8TransTime(SubTuesHour.Text.Trim());
            subchrgtime += u8TransTime(SubWenMin.Text.Trim()) + u8TransTime(SubWenHour.Text.Trim());
            subchrgtime += u8TransTime(SubThursMin.Text.Trim()) + u8TransTime(SubThursHour.Text.Trim());
            subchrgtime += u8TransTime(SubFriMin.Text.Trim()) + u8TransTime(SubFriHour.Text.Trim());
            subchrgtime += u8TransTime(SubSaturMin.Text.Trim()) + u8TransTime(SubSaturHour.Text.Trim());
            subchrgtime += u8TransTime(SubSunMin.Text.Trim()) + u8TransTime(SubSunHour.Text.Trim());

            subchrgtime += u16TransTime(SubMonDur.Text.Trim());
            subchrgtime += u16TransTime(SubTuesDur.Text.Trim());
            subchrgtime += u16TransTime(SubWenDur.Text.Trim());
            subchrgtime += u16TransTime(SubThursDur.Text.Trim());
            subchrgtime += u16TransTime(SubFriDur.Text.Trim());
            subchrgtime += u16TransTime(SubSaturDur.Text.Trim());
            subchrgtime += u16TransTime(SubSunDur.Text.Trim());

            subchrgtime += CheckSum(subchrgtime) + "0D0A";

            subchrgtime = "0D052A" + subchrgtime;
            ClientSendMsg(subchrgtime, 2);
            txtClientRecMsg.AppendText("周期预约充电：" + subchrgtime + "\r\n");
        }

        private void btDiscount_Click(object sender, EventArgs e)
        {
            timer1.Start();
            string discounttime = null;

            btDiscount.Enabled = false;

            discounttime += "C20024010002";

            discounttime += u8TransTime(DisStMin.Text.Trim()) + u8TransTime(DisStHour.Text.Trim());
            discounttime += u8TransTime(DisEndMin.Text.Trim()) + u8TransTime(DisEndHour.Text.Trim());

            discounttime += "0000000000000000000000000000000000000000000000000000000000";

            discounttime += CheckSum(discounttime) + "0D0A";
            discounttime = "0D052A" + discounttime;
            ClientSendMsg(discounttime, 2);
            txtClientRecMsg.AppendText("优惠电价充电：" + discounttime + "\r\n");
        }

        private void btDep_Click(object sender, EventArgs e)
        {
            timer1.Start();
            string deptime = null;
            int u8dayofWeek = 0;
            byte u8Temp = 16;

            btDep.Enabled = false;

            for (byte loop = 0; loop < 7; loop++)
            {
                if (true == DepWeekEnable.GetItemChecked(loop))
                {
                    u8dayofWeek |= (1 << loop);
                }
            }

            deptime += "C4001201";

            if (u8Temp < u8dayofWeek)
            {
                deptime += Convert.ToString(u8dayofWeek, 16);
            }
            else
            {
                deptime += "0" + Convert.ToString(u8dayofWeek, 16);
            }

            deptime += u8TransTime(DepMonMin.Text.Trim()) + u8TransTime(DepMonHour.Text.Trim());
            deptime += u8TransTime(DepTuesMin.Text.Trim()) + u8TransTime(DepTuesHour.Text.Trim());
            deptime += u8TransTime(DepWenMin.Text.Trim()) + u8TransTime(DepWenHour.Text.Trim());
            deptime += u8TransTime(DepThursMin.Text.Trim()) + u8TransTime(DepThursHour.Text.Trim());
            deptime += u8TransTime(DepFriMin.Text.Trim()) + u8TransTime(DepFriHour.Text.Trim());
            deptime += u8TransTime(DepSaturMin.Text.Trim()) + u8TransTime(DepSaturHour.Text.Trim());
            deptime += u8TransTime(DepSunMin.Text.Trim()) + u8TransTime(DepSunHour.Text.Trim());

            if (CheckState.Checked == CabinWarmEnable.CheckState)
            {
                deptime += "7F";
            }
            else
            {
                deptime += "00";
            }
            if (CheckState.Checked == BatWarmEnable.CheckState)
            {
                deptime += "01";
            }
            else
            {
                deptime += "00";
            }

            deptime += CheckSum(deptime) + "0D0A";
            deptime = "0D052A" + deptime;
            ClientSendMsg(deptime, 2);
            txtClientRecMsg.AppendText("预约出行：" + deptime + "\r\n");
        }

        private void btChrgSync_Click(object sender, EventArgs e)
        {
            string msg = null;
            msg = "0D052AC20024020000000000000000000000000000000000000000000000000000000000000000000000E40D0A";
            ClientSendMsg(msg, 1);
        }

        private void btDepSync_Click(object sender, EventArgs e)
        {
            string msg = null;
            msg = "0D052AC40012020000000000000000000000000000000000D40D0A";
            ClientSendMsg(msg, 1);
        }
        private void Combox_Reset(byte type)
        {
            switch (type)
            {
                case 0:
                {
                        targetSoc.ResetText();
                }
                break;
                case 1:
                {
                        SubMonHour.ResetText();
                        SubMonMin.ResetText();
                        SubTuesHour.ResetText();
                        SubTuesMin.ResetText();
                        SubWenHour.ResetText();
                        SubWenMin.ResetText();
                        SubThursHour.ResetText();
                        SubThursMin.ResetText();
                        SubFriHour.ResetText();
                        SubFriMin.ResetText();
                        SubSaturHour.ResetText();
                        SubSaturMin.ResetText();
                        SubSunHour.ResetText();
                        SubSunMin.ResetText();
                    }
                break;
                case 2:
                {
                        DisStHour.ResetText();
                        DisStMin.ResetText();
                        DisEndHour.ResetText();
                        DisEndMin.ResetText();
                    }
                break;
                case 3:
                {
                        DepMonHour.ResetText();
                        DepMonMin.ResetText();
                        DepTuesHour.ResetText();
                        DepTuesMin.ResetText();
                        DepWenHour.ResetText();
                        DepWenMin.ResetText();
                        DepThursHour.ResetText();
                        DepThursMin.ResetText();
                        DepFriHour.ResetText();
                        DepFriMin.ResetText();
                        DepSaturHour.ResetText();
                        DepSaturMin.ResetText();
                        DepSunHour.ResetText();
                        DepSunMin.ResetText();
                 }
                 break;
            }
        }
        private void GetChrgSyncData(byte[] arrRecMsg)
        {
            Combox_Reset(0);
            targetSoc.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 14, 2).Trim(), 16));
            //填入使能时间
            for (byte loop = 0; loop < 7; loop++)
            {
                int u8Temp = Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 26, 2).Trim(), 16) >> loop & 1;
                if (1 == u8Temp)
                {
                    ChrgWeekEnable.SetItemChecked(loop, true);
                }
                else
                {
                    ChrgWeekEnable.SetItemChecked(loop, false);
                }
            }
            //填入预约时间
            Combox_Reset(1);
            SubMonHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 30, 2).Trim(), 16));
            SubMonMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 28, 2).Trim(), 16));
            SubTuesHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 34, 2).Trim(), 16));
            SubTuesMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 32, 2).Trim(), 16));
            SubWenHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 38, 2).Trim(), 16));
            SubWenMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 36, 2).Trim(), 16));
            SubThursHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 42, 2).Trim(), 16));
            SubThursMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 40, 2).Trim(), 16));
            SubFriHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 46, 2).Trim(), 16));
            SubFriMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 44, 2).Trim(), 16));
            SubSaturHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 50, 2).Trim(), 16));
            SubSaturMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 48, 2).Trim(), 16));
            SubSunHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 54, 2).Trim(), 16));
            SubSunMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 52, 2).Trim(), 16));

            SubMonDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 56, 4).Trim(), 16));
            SubTuesDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 60, 4).Trim(), 16));
            SubWenDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 64, 4).Trim(), 16));
            SubThursDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 68, 4).Trim(), 16));
            SubFriDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 72, 4).Trim(), 16));
            SubSaturDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 76, 4).Trim(), 16));
            SubSunDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 80, 4).Trim(), 16));
            Combox_Reset(2);
            DisStHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 20, 2).Trim(), 16));
            DisStMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 18, 2).Trim(), 16));
            DisEndHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 24, 2).Trim(), 16));
            DisEndMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 22, 2).Trim(), 16));

            /*
            switch (arrRecMsg[17])
            {
                case 0x30:
                {
                        Combox_Reset(0);
                        targetSoc.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 14, 2).Trim(), 16));
                    
                }
                break;
                case 0x31:
                {
                        //填入使能时间
                        for (byte loop = 0; loop < 7; loop++)
                        {
                            int u8Temp = Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 26, 2).Trim(), 16) >> loop & 1;
                            if (1 == u8Temp)
                            {
                                ChrgWeekEnable.SetItemChecked(loop, true);
                            }
                            else
                            {
                                ChrgWeekEnable.SetItemChecked(loop, false);
                            }
                        }
                        //填入预约时间
                        Combox_Reset(1);
                        SubMonHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 30, 2).Trim(), 16));
                        SubMonMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 28, 2).Trim(), 16));
                        SubTuesHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 34, 2).Trim(), 16));
                        SubTuesMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 32, 2).Trim(), 16));
                        SubWenHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 38, 2).Trim(), 16));
                        SubWenMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 36, 2).Trim(), 16));
                        SubThursHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 42, 2).Trim(), 16));
                        SubThursMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 40, 2).Trim(), 16));
                        SubFriHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 46, 2).Trim(), 16));
                        SubFriMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 44, 2).Trim(), 16));
                        SubSaturHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 50, 2).Trim(), 16));
                        SubSaturMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 48, 2).Trim(), 16));
                        SubSunHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 54, 2).Trim(), 16));
                        SubSunMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 52, 2).Trim(), 16));

                        SubMonDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 56, 4).Trim(), 16));
                        SubTuesDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 60, 4).Trim(), 16));
                        SubWenDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 64, 4).Trim(), 16));
                        SubThursDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 68, 4).Trim(), 16));
                        SubFriDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 72, 4).Trim(), 16));
                        SubSaturDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 76, 4).Trim(), 16));
                        SubSunDur.Text = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 80, 4).Trim(), 16));
                }
                break;
                case 0x32:
                {
                        Combox_Reset(2);
                        DisStHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 20, 2).Trim(), 16));
                        DisStMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 18, 2).Trim(), 16));
                        DisEndHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 24, 2).Trim(), 16));
                        DisEndMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 22, 2).Trim(), 16));
                    }
                break;
            }
            */
        }

        private void GetDepSyncData(byte[] arrRecMsg)
        {
            for (byte loop = 0; loop < 7; loop++)
            {
                int u8Temp = Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 14, 2).Trim(), 16) >> loop & 1;
                if (1 == u8Temp)
                {
                    DepWeekEnable.SetItemChecked(loop, true);
                }
                else
                {
                    DepWeekEnable.SetItemChecked(loop, false);
                }
            }

            if (127 == Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 44, 2).Trim(), 16))
            {
                CabinWarmEnable.CheckState = CheckState.Checked;
            }
            else
            {
                CabinWarmEnable.CheckState = CheckState.Unchecked;
            }

            if (1 == Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 46, 2).Trim(), 16))
            {
                BatWarmEnable.CheckState = CheckState.Checked;
            }
            else
            {
                BatWarmEnable.CheckState = CheckState.Unchecked;
            }

            Combox_Reset(3);
            DepMonHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 18, 2).Trim(), 16));
            DepMonMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 16, 2).Trim(), 16));
            DepTuesHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 22, 2).Trim(), 16));
            DepTuesMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 20, 2).Trim(), 16));
            DepWenHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 26, 2).Trim(), 16));
            DepWenMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 24, 2).Trim(), 16));
            DepThursHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 30, 2).Trim(), 16));
            DepThursMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 28, 2).Trim(), 16));
            DepFriHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 34, 2).Trim(), 16));
            DepFriMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 32, 2).Trim(), 16));
            DepSaturHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 38, 2).Trim(), 16));
            DepSaturMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 36, 2).Trim(), 16));
            DepSunHour.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 42, 2).Trim(), 16));
            DepSunMin.SelectedText = Convert.ToString(Convert.ToInt32(Encoding.UTF8.GetString(arrRecMsg, 40, 2).Trim(), 16));


        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            timeout++;
            if (5 < timeout)
            {
                timeout = 0;
                timer1.Stop();
                HearBreak.Stop();
                socketClient.Close();
                btnListenServer.Text = "Connect";
                bSwitch = false;
                btSoc.Enabled = true;
                btSubSet.Enabled = true;
                btDiscount.Enabled = true;
                btDep.Enabled = true;
                MessageBox.Show("当前设置超时,请重试");
            }
        }
    }

}
