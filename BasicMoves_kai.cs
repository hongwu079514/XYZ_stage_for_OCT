
// Example 3 Basic Moves Rev 4
//
// This program demonstrates how to perform basic trap profile move.
//
// This program demonstrates the following types of motion:
// 1. Trapezoidal move 
// 2. Halt move
//
// This program assumes the following axis configuration:
// 1. Upon startup it will enable axis at Can Node ID 1.
// 2. The motor has an encoder with an index
//
// This code also includes the following prerequisites:
// 1. The amplifier and motor must be preconfigured and set up properly to run.
// 2. The hardware enable switch must be installed and easily accessible
//
// As with any motion product extreme caution must used! Read and understand
// all parameter settings before attemping to send to amplifier.
//
//
// Copley Motion Objects are Copyright, 2002-2018, Copley Controls.
//
// For more information on Copley Motion products see:
// http://www.copleycontrols.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CMLCOMLib;
using System.Threading;
using System.IO;
using LabJack.LabJackUD;






//----------------------end new program------------------------

namespace EX3_BasicMoves
{

    
  

    public partial class BasicMoves : Form
    {
        //***************************************************
        //
        //  CANOpen Network
        //
        //***************************************************
        const int X_AXIS_CAN_ADDRESS = 1;
        const int X2_AXIS_CAN_ADDRESS = 2;
        const int Y_AXIS_CAN_ADDRESS = 3;
        const int Z_AXIS_CAN_ADDRESS = 4;

        // comment by YZY:  before is the address for each 4 axis


        canOpenObj canOpen;

        const int LOG_ALL = 99;
        CopleyMotionLibraryObj cmlObj;

        AmpObj xAxisAmp;
        AmpObj x2AxisAmp;
        AmpObj yAxisAmp;
        AmpObj zAxisAmp;
        ProfileSettingsObj ProfileSettings_x;
        ProfileSettingsObj ProfileSettings_x2;
        ProfileSettingsObj ProfileSettings_y;
        ProfileSettingsObj ProfileSettings_z;
        HomeSettingsObj Home;

        int off_set = -10000;
        // before is the initiliztion of varibales, need to mention that off_set means after each "home", axis will move on the relative way for paticular meters



        // Create a delegate to close down the application in a thread safe way
        delegate void CloseApp();

        public BasicMoves()
        {
            InitializeComponent();
        }


        // ---------------------------------------labjack-----------------
        class U3_EFunctions
        {
            // our U3 variable
            private U3 u3;



            // If error occured print a message indicating which one occurred. If the error is a group error (communication/fatal), quit
            public void showErrorMessage(LabJackUDException e) // related to labjack( led to show command)
            {
                Console.Out.WriteLine("Error: " + e.ToString());
                if (e.LJUDError > U3.LJUDERROR.MIN_GROUP_ERROR)
                {
                    Console.ReadLine(); // Pause for the user
                    Environment.Exit(-1);
                }
            }

            public void performActions() // fuction for labjack to fullfil
            {
                double dblValue = 0;
                int intValue = 0;

                int binary;
                int[] aEnableTimers = new int[2];
                int[] aEnableCounters = new int[2];
                int tcpInOffset;
                int timerClockDivisor;
                LJUD.TIMERCLOCKS timerClockBaseIndex;
                int[] aTimerModes = new int[2];
                double[] adblTimerValues = new double[2];
                int[] aReadTimers = new int[2];
                int[] aUpdateResetTimers = new int[2];
                int[] aReadCounters = new int[2];
                int[] aResetCounters = new int[2];
                double[] adblCounterValues = { 0, 0 };
                double highTime, lowTime, dutyCycle;

                try
                {
                    //Open the first found LabJack U3.
                    u3 = new U3(LJUD.CONNECTION.USB, "0", true); // Connection through USB

                    //Start by using the pin_configuration_reset IOType so that all
                    //pin assignments are in the factory default condition.
                    LJUD.ePut(u3.ljhandle, LJUD.IO.PIN_CONFIGURATION_RESET, 0, 0, 0);



                    //Set DAC0 to 3.0 volts.
                    dblValue = 3.0;

                    binary = 0;
                    //LJUD.eDAC(u3.ljhandle, 0, dblValue, binary, 0, 0);
                    Console.Out.WriteLine("DAC0 set to {0:0.###} volts\n", dblValue);


                    //wait 1 second
                    Thread.Sleep(1000); //Wait 1 second.
                                        //off
                    LJUD.eDAC(u3.ljhandle, 0, 0, binary, 0, 0);

                    //wait 1 second
                    Thread.Sleep(1000); //Wait 1 second.
                                        //on
                    LJUD.eDAC(u3.ljhandle, 0, dblValue, binary, 0, 0);

                    //wait 1 second
                    Thread.Sleep(1000); //Wait 1 second.
                                        //off
                    LJUD.eDAC(u3.ljhandle, 0, 0, binary, 0, 0);

                    Console.Out.WriteLine("DAC0 set111111 to 0 volts\n");
                }
                catch (LabJackUDException e)
                {
                    showErrorMessage(e);
                }
            }
        }
        // ---------------------------------------labjack-----------------


        private void BasicMoves_Load(object sender, EventArgs e) // initializtion of basicmove variables
        {
            try
            {
                //**************************************************************************
                //* Turn on logging by setting a CML object
                //**************************************************************************
                cmlObj = new CopleyMotionLibraryObj();
                cmlObj.DebugLevel = LOG_ALL;

                //Initialize code here
                // create an AmpObj type varible will have several steps, step 1 is below
                xAxisAmp = new AmpObj();
                x2AxisAmp = new AmpObj();
                yAxisAmp = new AmpObj();
                zAxisAmp = new AmpObj();


                //***************************************************
                //
                //  CANOpen Network
                //
                //***************************************************
                canOpen = new canOpenObj();
                //
                //**************************************************************************
               
                //* then the default bit rate (1 Mbit per second) is used.  If no port name
                //* is specified, then CMO will use the first supported CAN card found and
                //* use channel 0 of that card.
                //**************************************************************************
                // Set the bit rate to 1 Mbit per second
                canOpen.BitRate = CML_BIT_RATES.BITRATE_1_Mbit_per_sec;
                // Indicate that channel 0 of a Copley CAN card should be used
                canOpen.PortName = "copley0";
                //***************************************************
                //* Initialize the CAN card and network
                //***************************************************
                canOpen.Initialize();
                //***************************************************
                //* Initialize the amplifier
                //***************************************************

                //step2: initialize amplifer
                xAxisAmp.Initialize(canOpen, X_AXIS_CAN_ADDRESS);
                x2AxisAmp.Initialize(canOpen, X2_AXIS_CAN_ADDRESS);
                yAxisAmp.Initialize(canOpen, Y_AXIS_CAN_ADDRESS);
                zAxisAmp.Initialize(canOpen, Z_AXIS_CAN_ADDRESS);





                // Read velocity loop settings from the amp, use these as reasonble starting
                // points for the trajectory limits.
                VelocityTextBox.Text = Convert.ToString((xAxisAmp.VelocityLoopSettings.VelLoopMaxVel) / 10);
                AccelTextBox.Text = Convert.ToString((xAxisAmp.VelocityLoopSettings.VelLoopMaxAcc) / 10);
                DecelTextBox.Text = Convert.ToString((xAxisAmp.VelocityLoopSettings.VelLoopMaxDec) / 10);

                // Initialize home object with amplifier home settings 
                Home = xAxisAmp.HomeSettings;

                Timer1.Start();
            }
            catch (Exception ex)
            {
                DisplayError(ex);
            }
        }


        private void HomeAxisButton_Click(object sender, EventArgs e)// function realted to home
        {
            try
            {
                CML_EVENT_STATUS current_xHome_status = 0; // the sensor will send signal back if axis reach the limit

                if ((CML_EVENT_STATUS)(current_xHome_status & CML_EVENT_STATUS.EVENT_STATUS_POSITIVE_LIMIT) == CML_EVENT_STATUS.EVENT_STATUS_POSITIVE_LIMIT)
                {
                    HomeAxisButton.Enabled = false;
                }

                else
                {
                    HomeAxisButton.Enabled = false;
                    // below is the speed parament related to home
                    Home.HomeVelFast = 4000;
                    Home.HomeVelSlow = 4000;
                    Home.HomeAccel = 1000;
                    Home.HomeMethod = CML_HOME_METHOD.CHOME_POSITIVE_LIMIT; // Set the home location to the positive limit sensor
                    Home.HomeOffset = 0;


                    // read profile settings from amp
                    ProfileSettings_x = xAxisAmp.ProfileSettings;
                    ProfileSettings_x2 = x2AxisAmp.ProfileSettings;

                    //Set the profile type for move
                    ProfileSettings_x.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;
                    ProfileSettings_x2.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;

                    //Set the profile trajectory values 
                    ProfileSettings_x.ProfileAccel = Convert.ToDouble(AccelTextBox.Text);
                    ProfileSettings_x.ProfileDecel = Convert.ToDouble(DecelTextBox.Text);
                    ProfileSettings_x.ProfileVel = Convert.ToDouble(VelocityTextBox.Text);

                    ProfileSettings_x2.ProfileAccel = Convert.ToDouble(AccelTextBox.Text);
                    ProfileSettings_x2.ProfileDecel = Convert.ToDouble(DecelTextBox.Text);
                    ProfileSettings_x2.ProfileVel = Convert.ToDouble(VelocityTextBox.Text);

                    ProfileSettings_x = xAxisAmp.ProfileSettings; // read profile settings from amp
                    xAxisAmp.MoveRel(2000000);

                    ProfileSettings_x2 = x2AxisAmp.ProfileSettings;
                    x2AxisAmp.MoveRel(2000000);

                    //y go home
                    Home.HomeVelFast = (yAxisAmp.VelocityLoopSettings.VelLoopMaxVel) / 10;
                    Home.HomeVelSlow = (yAxisAmp.VelocityLoopSettings.VelLoopMaxVel) / 15;
                    Home.HomeAccel = yAxisAmp.VelocityLoopSettings.VelLoopMaxAcc / 10;
                    Home.HomeMethod = CML_HOME_METHOD.CHOME_INDEX_POSITIVE;
                    Home.HomeOffset = 0;
                    yAxisAmp.HomeSettings = Home;
                    yAxisAmp.GoHome();



                    CML_EVENT_STATUS xAxis_stop_status = 0; // the sensor will send signal back if axis reach the limit
                    bool flag = true;

                    //x2AxisAmp.ReadEventStatus(ref xAxis_stop_status);

                    Boolean stopClick = true;

                    // constantly checking if x reaching the limit
                    while (flag && stopClick)
                    {
                        xAxisAmp.ReadEventStatus(ref xAxis_stop_status);
               

                        // Checking if the x1 sensor is active
                        if ((CML_EVENT_STATUS)(xAxis_stop_status & CML_EVENT_STATUS.EVENT_STATUS_POSITIVE_LIMIT) == CML_EVENT_STATUS.EVENT_STATUS_POSITIVE_LIMIT)
                        {
                            flag = false; // if the limit is reached, breaks from loop
                        }

                    }
                    // halt the move of both x axles 
                    x2AxisAmp.HaltMove();
                    xAxisAmp.HaltMove();


                    doMoveButton.Enabled = true;

                    // HomeAxisButton.Enabled = true;
                }


            }
            catch (Exception ex)
            {
                HomeAxisButton.Enabled = true;
                DisplayError(ex);
            }
        }


        private void enableButton_Click(object sender, EventArgs e)//enable or disable the amplifier
        {
            try
            {
                if (enableButton.Text == "Amp Disable")
                {
                    xAxisAmp.Disable();
                    enableButton.Text = "Amp Enable";
                }
                else
                {
                    xAxisAmp.Enable();
                    enableButton.Text = "Amp Disable";
                }
            }
            catch (Exception ex)
            {
                DisplayError(ex);
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                //Read and display actual position 
                posTextBox.Text = Convert.ToString(xAxisAmp.PositionActual);
                y_postextbox.Text = Convert.ToString(yAxisAmp.PositionActual);
            }
            catch (Exception ex)
            {
                Timer1.Stop();
                DisplayError(ex);
            }
        }


        // Moving to a set position function
        private void doMoveButton_Click(object sender, EventArgs e) 
        {
            try
            {
                
                int Distance_x;
                int Distance_y;
                int flag = 1;
                int step_x;
                int step_y;

                int delay_Time = 5000;


                // a temp varible to document the input value for x axis to move
                Distance_x = Convert.ToInt32(X_DistanceTextBox.Text);
                Distance_y = Convert.ToInt32(Y_DistanceTextBox.Text);

                int tmp = Distance_y;

                // a temp varible to document the input value for x axis to move each time
                step_x = Convert.ToInt32(X_StepTextBox.Text); 
                step_y = Convert.ToInt32(Y_StepTextBox.Text);


                //step 1
                ProfileSettings_x = xAxisAmp.ProfileSettings; // read profile settings from amp
                ProfileSettings_x2 = x2AxisAmp.ProfileSettings;
                ProfileSettings_y = yAxisAmp.ProfileSettings;

                //Set the profile type for move, step2
                ProfileSettings_x.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;
                ProfileSettings_x2.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;
                ProfileSettings_y.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;
                //Set the profile trajectory values , step3
                ProfileSettings_x.ProfileAccel = Convert.ToDouble(AccelTextBox.Text);
                ProfileSettings_x.ProfileDecel = Convert.ToDouble(DecelTextBox.Text);
                ProfileSettings_x.ProfileVel = Convert.ToDouble(VelocityTextBox.Text);

                ProfileSettings_x2.ProfileAccel = Convert.ToDouble(AccelTextBox.Text);
                ProfileSettings_x2.ProfileDecel = Convert.ToDouble(DecelTextBox.Text);
                ProfileSettings_x2.ProfileVel = Convert.ToDouble(VelocityTextBox.Text);

                ProfileSettings_y.ProfileAccel = Convert.ToDouble(AccelTextBox.Text);
                ProfileSettings_y.ProfileDecel = Convert.ToDouble(DecelTextBox.Text);
                ProfileSettings_y.ProfileVel = Convert.ToDouble(VelocityTextBox.Text);

                // Update the amplier's profile settigns, step4
                xAxisAmp.ProfileSettings = ProfileSettings_x;
                x2AxisAmp.ProfileSettings = ProfileSettings_x2;
                yAxisAmp.ProfileSettings = ProfileSettings_y;


                // Execute the move need to change
                int i = 1;
                int i_2 = 1;
                
                // write the running output to excel
                string filePath = @"C:\Users\ljw5330\Desktop\Position.csv";
                string stringLine;
                string stringHeader = "x,y,z"+"\r\n";

                File.WriteAllText(filePath, stringHeader);

                U3_EFunctions a = new U3_EFunctions();// related to led

                while (Distance_x > 0)// below is the loop for xy to move in a certain pattern
                {
       
                    Console.WriteLine("current while loop is " + i);// log message
                    i += 1;
                    if (flag == 1)
                    {
                        if (Distance_y > 0) 
                        {
                            // write position data to file
                            string x_position = Convert.ToString(xAxisAmp.PositionActual);
                            string y_position = Convert.ToString(yAxisAmp.PositionActual);
                            string z_position = Convert.ToString(zAxisAmp.PositionActual);

                            stringLine = x_position + "," + y_position + "," + z_position + "\r\n";
                            //write xyz positoon to file
                            File.AppendAllText(filePath, stringLine);

                            // Flash LED
                            a.performActions();
                            Console.WriteLine("y pos.");
                            yAxisAmp.MoveRel(-step_y);

                            //wait
                            
                            Thread.Sleep(delay_Time);
                            

                            Distance_y -= step_y;
                            Console.WriteLine("current distance_y == "+ Distance_y);
                            
                            Console.WriteLine(" current distance_x == "+ Distance_x);
                           
                            i_2 += 1;

                        }
                        // Move x
                        if (Distance_y <= 0)// when Y finsish its move, X should move 1 step
                        {
                            flag = -1;
                            Distance_y = tmp;

                            // Flash LED
                            a.performActions();
                            Console.WriteLine("x move y pos.");

                            xAxisAmp.MoveRel(-step_x);
                            x2AxisAmp.MoveRel(-step_x);

                            //Console.ReadKey();
                            Thread.Sleep(delay_Time);

                            Distance_x -= step_x;
                            Console.WriteLine(" current distance_y == "+ Distance_y);
                            Console.WriteLine(" X moved,y going negative  " );

                            Console.WriteLine(" ycurrent distance_x == "+ Distance_x);


                        }
                    }
                    else// when flag not equall to 1, means a positive way
                    {
                        // Flash LED
                        a.performActions();
                        Console.WriteLine("y neg.");

                        yAxisAmp.MoveRel(step_y);
                        //wait
                        // Console.ReadKey();
                        Thread.Sleep(delay_Time);
                        //yAxisAmp.WaitMoveDone(100000);

                        Distance_y -= step_y;
                        Console.WriteLine(" 2ndcurrent distance_y == "+ Distance_y);
                       
                        Console.WriteLine(" 2ndcurrent distance_x == "+ Distance_x);

                        // Move x
                        if (Distance_y <= 0)
                        {
                            flag = 1;
                            Distance_y = tmp;
                            // Flash LED
                            a.performActions();
                            Console.WriteLine("x move y neg.");

                            xAxisAmp.MoveRel(-step_x);
                            x2AxisAmp.MoveRel(-step_x);

                            // Console.ReadKey();
                            Thread.Sleep(delay_Time);

                            Distance_x -= step_x;
                            Console.WriteLine(" current distance_y == "+ Distance_y);
                            Console.WriteLine(" X moved,y going postive  ");
                            Console.WriteLine(" current distance_x == "+ Distance_x);
                        
                        }
                    }
                }
                if(Distance_x <= 0)
                {
                    // Flash LED
                    a.performActions();

                    Console.WriteLine(" xxxx reach the finish line ");
                }
                HomeAxisButton.Enabled = true;
            }


            catch (Exception ex)
            {
                doMoveButton.Enabled = true;
                HomeAxisButton.Enabled = true;
                DisplayError(ex);
            }
        }


        

        private void Modified_Button_Click(object sender, EventArgs e)// function realted to modified move, means move to a paticular position
        {


            int Modified_Distance_x = Convert.ToInt32(Modified_X_Textbox.Text);
            int Modified_Distance_y = Convert.ToInt32(Modified_Y_Textbox.Text);

            ProfileSettings_x = xAxisAmp.ProfileSettings; // read profile settings from amp,step1
            ProfileSettings_x2 = x2AxisAmp.ProfileSettings;

            ProfileSettings_y = yAxisAmp.ProfileSettings;

            //Set the profile type for move, step2
            ProfileSettings_x.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;
            ProfileSettings_x2.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;
            ProfileSettings_y.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;

            //Set the profile trajectory values , step3
            ProfileSettings_x.ProfileAccel = Convert.ToDouble(AccelTextBox.Text);
            ProfileSettings_x.ProfileDecel = Convert.ToDouble(DecelTextBox.Text);
            ProfileSettings_x.ProfileVel = Convert.ToDouble(VelocityTextBox.Text);

            ProfileSettings_x2.ProfileAccel = Convert.ToDouble(AccelTextBox.Text);
            ProfileSettings_x2.ProfileDecel = Convert.ToDouble(DecelTextBox.Text);
            ProfileSettings_x2.ProfileVel = Convert.ToDouble(VelocityTextBox.Text);

            ProfileSettings_y.ProfileAccel = Convert.ToDouble(AccelTextBox.Text);
            ProfileSettings_y.ProfileDecel = Convert.ToDouble(DecelTextBox.Text);
            ProfileSettings_y.ProfileVel = Convert.ToDouble(VelocityTextBox.Text);

            // Update the amplier's profile settigns, step4
            xAxisAmp.ProfileSettings = ProfileSettings_x;
            x2AxisAmp.ProfileSettings = ProfileSettings_x2;
            yAxisAmp.ProfileSettings = ProfileSettings_y;
            
            // move
            xAxisAmp.MoveRel(Modified_Distance_x);
            x2AxisAmp.MoveRel(Modified_Distance_x);
            yAxisAmp.MoveRel(Modified_Distance_y);


            HomeAxisButton.Enabled = true;

        }

        private void Move_Z_Up_Button_Click(object sender, EventArgs e)// this function is to lift up Z axis
        {
            ProfileSettings_z = zAxisAmp.ProfileSettings; // read profile settings from amp

            //Set the profile type for move
            ProfileSettings_z.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;

            //Set the profile trajectory values 
            ProfileSettings_z.ProfileAccel = 10000;
            ProfileSettings_z.ProfileDecel = 10000;
            ProfileSettings_z.ProfileVel = 4000;

            

            // Update the amplier's profile settigns
            zAxisAmp.ProfileSettings = ProfileSettings_z;

            zAxisAmp.MoveRel(-500);

        }

        private void Move_Z_down_Button_Click(object sender, EventArgs e) // lift down  Z axis
        {

            ProfileSettings_z = zAxisAmp.ProfileSettings; // read profile settings from amp

            //Set the profile type for move
            ProfileSettings_z.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;

            //Set the profile trajectory values 
            ProfileSettings_z.ProfileAccel = 10000;
            ProfileSettings_z.ProfileDecel = 10000;
            ProfileSettings_z.ProfileVel = 4000;



            // Update the amplier's profile settigns
            zAxisAmp.ProfileSettings = ProfileSettings_z;

            zAxisAmp.MoveRel(500);
        }

        private void haltMoveButton_Click(object sender, EventArgs e)// stop moving during any movement
        {
            try
            {
                doMoveButton.Enabled = false;
                HomeAxisButton.Enabled = false;

                //set halt mode desired before halting the move
                xAxisAmp.HaltMode = CML_HALT_MODE.HALT_DECEL;
                x2AxisAmp.HaltMode = CML_HALT_MODE.HALT_DECEL;
                yAxisAmp.HaltMode = CML_HALT_MODE.HALT_DECEL;

                // now halt the move
                xAxisAmp.HaltMove();
                x2AxisAmp.HaltMove();
                yAxisAmp.HaltMove();


                doMoveButton.Enabled = true;

                HomeAxisButton.Enabled = true;
                
                
             

            }
            catch (Exception ex)
            {
                doMoveButton.Enabled = true;
                HomeAxisButton.Enabled = true;
                DisplayError(ex);
            }
        }

        private void BasicMoves_FormClosing(object sender, FormClosingEventArgs e)
        {
            xAxisAmp.Disable();
            x2AxisAmp.Disable();
            yAxisAmp.Disable();

        }

        public void DisplayError(Exception ex)
        {
            DialogResult errormsgbox;
            errormsgbox = MessageBox.Show("Error Message: " + ex.Message + "\n" + "Error Source: "
                + ex.Source, "CMO Error", MessageBoxButtons.OKCancel);
            if (errormsgbox == DialogResult.Cancel)
            {
                // it is possible that this method was called from a thread other than the 
                // GUI thread - if this is the case we must use a delegate to close the application.
                //Dim d As New CloseApp(AddressOf ThreadSafeClose)
                CloseApp d = new CloseApp(ThreadSafeClose);
                this.Invoke(d);
            }
        }

        public void ThreadSafeClose()
        {
            //If the calling thread is different than the GUI thread, then use the
            //delegate to close the application, otherwise call close() directly
            if (this.InvokeRequired)
            {
                CloseApp d = new CloseApp(ThreadSafeClose);
                this.Invoke(d);
            }
            else
                Close();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void DistanceTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void label18_Click(object sender, EventArgs e)
        {

        }

        private void Modified_X_Textbox_TextChanged(object sender, EventArgs e)
        {

        }

        private void VelocityTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void AccelTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        // Perform scan once
        private void button1_Click(object sender, EventArgs e)
        {
            U3_EFunctions a = new U3_EFunctions();
            a.performActions();
        }

        private void label21_Click(object sender, EventArgs e)
        {

        }

        private void label24_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void X_StepTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void posTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        private void y_postextbox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

