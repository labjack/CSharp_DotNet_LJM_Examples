//-----------------------------------------------------------------------------
// ThermocoupleExampleAINEF.cs
//
// Demonstrates thermocouple configuration and measurement using the
// thermocouple AIN_EF features. This example is for devices such as
// the T7 and T8 that support built-in amplification and the thermocouple
// AIN_EF. For devices without built-in amplification, such as the T4,
// see 'ThermocoupleExampleInAmp' instead.
//
// support@labjack.com
//
// Relevant Documentation:
//
// Thermocouple App-Note:
//      https://labjack.com/support/app-notes/thermocouples
//
// LJM Library:
//     LJM Library Installer:
//         https://labjack.com/support/software/installers/ljm
//     LJM Users Guide:
//         https://labjack.com/support/software/api/ljm
//     Opening and Closing:
//         https://labjack.com/support/software/api/ljm/function-reference/opening-and-closing
//     Single Value Functions (such as eWriteName):
//         https://labjack.com/support/software/api/ljm/function-reference/single-value-functions
//     Multiple Value Functions (such as eWriteNames and eReadNames):
//         https://labjack.com/support/software/api/ljm/function-reference/multiple-value-functions
//     TCVoltsToTemp:
//         https://labjack.com/support/software/api/ud/function-reference/tcvoltstotemp
//     Timing Functions (such as StartInterval, WaitForNextInterval and
//     CleanInterval):
//        https://labjack.com/support/software/api/ljm/function-reference/timing-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//     Thermocouple AIN_EF:
//         https://labjack.com/support/datasheets/t-series/ain/extended-features/thermocouple
//-----------------------------------------------------------------------------

using System;
using LabJack;

namespace ThermocoupleExampleAINEF
{
    //Enum representing the temperature units for AIN_EF thermocouple readings.
    public enum TempUnits {
        DEGK = 'K',
        DEGC = 'C',
        DEGF = 'F',
    }

    //Enum representing the thermocouple type (AIN#_EF_INDEX).
    public enum TCType {
        E = 20,
        J = 21,
        K = 22,
        R = 23,
        T = 24,
        S = 25,
        N = 27,
        B = 28,
        C = 30
    }

    public class ThermocoupleAINEF
    {
        //Note that the TCType enum values do not align with the LJM constants
        //such as LJM_ttK. The LJM constants are only for use with the
        //TCVoltsToTemp function, which is not necessary when using the AIN_EF.
        private TCType tcType;

        //If taking a differential reading on a T7, you only need to configure
        //the AIN_EF on the positive channel AIN.
        private string ainChannel;

        //Modbus name and address to read the CJC sensor at. The constructor
        //will set the address based on the name.
        private string cjcName;
        private int cjcAddress;

        //Slope of CJC voltage to Kelvin conversion (K/volt).
        //TEMPERATURE_DEVICE_K and TEMPERATURE# returns temp in K, so this
        //would be set to 1 if using it for CJC. If using an LM34 on some AIN
        //for CJC, this config should be 55.56.
        private float cjcSlope;

        //Offset for CJC temp (in Kelvin). This would normally be 0 if reading
        //the register TEMPERATURE_DEVICE_K or TEMPERATURE# for CJC. If using
        //an InAmp or expansion board, the CJ might be a bit cooler than the
        //internal temp sensor, so you might adjust the offset down a few
        //degrees. If using an LM34 on some AIN for CJC, this config should
        //be 255.37.
        private float cjcOffset;

        private TempUnits tempUnits;

        public ThermocoupleAINEF(
            TCType tcType,
            string ainChannel,
            string cjcName,
            float cjcSlope,
            float cjcOffset,
            TempUnits tempUnits
        ){
            int type = 0;

            this.tcType = tcType;
            this.ainChannel = ainChannel;
            this.cjcName = cjcName;
            this.cjcSlope = cjcSlope;
            this.cjcOffset = cjcOffset;
            this.tempUnits = tempUnits;
            //Getting cjcAddress from cjcName.
            LJM.NameToAddress(cjcName, ref this.cjcAddress, ref type);
        }

        public void setup(int handle)
        {
            const int NUM_FRAMES = 5;
            string[] aNames = new string[NUM_FRAMES];
            double[] aValues = new double[NUM_FRAMES];
            int errorAddress = -1;

            try
            {
                //For setting up the AIN#_EF_INDEX (thermocouple type).
                aNames[0] = this.ainChannel + "_EF_INDEX";
                aValues[0] = (double)this.tcType;

                //For setting up the AIN#_EF_CONFIG_A (temperature units).
                aNames[1] = this.ainChannel + "_EF_CONFIG_A";
                switch (this.tempUnits) {
                    case TempUnits.DEGK:
                        aValues[1] = 0;
                        break;
                    case TempUnits.DEGC:
                        aValues[1] = 1;
                        break;
                    case TempUnits.DEGF:
                        aValues[1] = 2;
                        break;
                    default:
                        aValues[1] = 0;
                        break;
                }

                //For setting up the AIN#_EF_CONFIG_B (CJC address).
                aNames[2] = this.ainChannel + "_EF_CONFIG_B";
                aValues[2] = this.cjcAddress;

                //For setting up the AIN#_EF_CONFIG_D (CJC slope).
                aNames[3] = this.ainChannel + "_EF_CONFIG_D";
                aValues[3] = this.cjcSlope;

                //For setting up the AIN#_EF_CONFIG_E (CJC offset).
                aNames[4] = this.ainChannel + "_EF_CONFIG_E";
                aValues[4] = this.cjcOffset;

                LJM.eWriteNames(
                    handle,
                    NUM_FRAMES,
                    aNames,
                    aValues,
                    ref errorAddress
                );
            }
            catch (Exception)
            {
                Console.Out.WriteLine("Exception in ThermocoupleAINEF.setup");
                throw;
            }
        }

        public (double tcTemp, double tcVolts, double cjcTemp) getReadings(int handle)
        {
            const int NUM_FRAMES = 3;
            string[] aNames = {
                this.ainChannel + "_EF_READ_A",
                this.ainChannel + "_EF_READ_B",
                this.ainChannel + "_EF_READ_C"
            };
            double[] aValues = new double[NUM_FRAMES];
            int errorAddress = -1;
            double tcTemp = 0, tcVolts = 0, cjcTemp = 0;

            try
            {
                LJM.eReadNames(
                    handle,
                    NUM_FRAMES,
                    aNames,
                    aValues,
                    ref errorAddress
                );
                tcTemp = aValues[0]; //Read value from AIN#_EF_READ_A.
                tcVolts = aValues[1]; //Read value from AIN#_EF_READ_B.
                cjcTemp = aValues[2]; //Read value from AIN#_EF_READ_C.
            }
            catch (Exception)
            {
                Console.Out.WriteLine(
                    "Exception in ThermocoupleAINEF.getReadings"
                );
                throw;
            }
            return (tcTemp, tcVolts, cjcTemp);
        }

        public TempUnits getTempUnits()
        {
            return this.tempUnits;
        }
    }

    class ThermocoupleExampleAINEF
    {
        static void Main(string[] args)
        {
            ThermocoupleExampleAINEF tce = new ThermocoupleExampleAINEF();
            tce.performActions();
        }
        public void performActions()
        {
            int ainNumber;
            string ainChannel;
            string cjcName;
            double ainRange;
            int ainRes;
            string[] aNames;
            double[] aValues;
            double tcTemp, tcVolts, cjcTemp;
            int handle = 0;
            int devType = 0;
            int conType = 0;
            int serNum = 0;
            int ipAddr = 0;
            int port = 0;
            int maxBytesPerMB = 0;
            string ipAddrStr = "";
            int intervalHandle = 1;
            int skippedIntervals = 0;
            int errorAddress = -1;

            try
            {
                //Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);  // Any device, Any connection, Any identifier.
                //LJM.OpenS("T8", "ANY", "ANY", ref handle);  // T8 device, Any connection, Any identifier.
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  // T7 device, Any connection, Any identifier.
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  // T4 device, Any connection, Any identifier.
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier.

                LJM.GetHandleInfo(
                    handle,
                    ref devType,
                    ref conType,
                    ref serNum,
                    ref ipAddr,
                    ref port,
                    ref maxBytesPerMB
                );
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine(
                    "Opened a LabJack with Device type: " + devType + ", " +
                    "Connection type: " + conType + ","
                );
                Console.WriteLine(
                    "Serial number: " + serNum + ", " +
                    "IP address: " + ipAddrStr + ", " +
                    "Port: " + port + ","
                );
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                ainNumber = 0; //Take a thermocouple reading on AIN0.
                ainChannel = "AIN" + ainNumber;
                if (devType == LJM.CONSTANTS.dtT8)
                {
                    //Use TEMPERATURE# for CJC if using a T8.
                    cjcName = "TEMPERATURE" + ainNumber;
                }
                else {
                    cjcName = "TEMPERATURE_DEVICE_K";
                }

                ThermocoupleAINEF ain0EF = new ThermocoupleAINEF(
                    TCType.K, //Type K thermocouple measurement.
                    ainChannel,
                    cjcName,
                    1, //CJC slope. 55.56 for LM34.
                    0, //CJC Offset. 255.37 for LM34.
                    TempUnits.DEGC //Output temperature values with units °C.
                );

                //Write the AIN_EF configurations to the device.
                ain0EF.setup(handle);

                //Configure the analog input.
                //If you set range according to the max TC voltage amplitude,
                //the device will use the best/closest setting available.
                ainRange = 0.07; //Will use ±0.1V on T7, ±0.075V on T8.
                ainRes = 0; //Default AIN resolution index.

                if (devType == LJM.CONSTANTS.dtT7)
                {
                    //There are only certain valid differential channel
                    //pairs. For AIN0-13 the valid pairs are an even
                    //numbered AIN and next odd AIN. For example,
                    //AIN0-AIN1, AIN2-AIN3. To take a differential reading
                    //between AIN0 and AIN1, set AIN0_NEGATIVE_CH to 1.
                    aNames = new string[3];
                    aValues = new double[3];
                    aNames[0] = ainChannel + "_RANGE";
                    aValues[0] = ainRange;
                    aNames[1] = ainChannel + "_RESOLUTION_INDEX";
                    aValues[1] = ainRes;
                    //Set up a single ended measurement.
                    aNames[2] = ainChannel + "_NEGATIVE_CH";
                    aValues[2] = LJM.CONSTANTS.GND;
                }
                else
                {
                    aNames = new string[2];
                    aValues = new double[2];
                    aNames[0] = ainChannel + "_RANGE";
                    aValues[0] = ainRange;
                    aNames[1] = ainChannel + "_RESOLUTION_INDEX";
                    aValues[1] = ainRes;

                }
                LJM.eWriteNames(
                    handle,
                    aNames.Length,
                    aNames,
                    aValues,
                    ref errorAddress
                );

                Console.WriteLine(
                    "\nReading thermocouple temperature in a loop." +
                    " Press a key to stop.\n"
                );
                LJM.StartInterval(intervalHandle, 1000000);

                while (!Console.KeyAvailable)
                {
                    (tcTemp, tcVolts, cjcTemp) = ain0EF.getReadings(handle);
                    Console.WriteLine(
                        "tcTemp: {0:N3} {1}," +
                        "\t tcVolts: {2:N6}," +
                        "\tCJTemp: {3:N3} {4}\n",
                        tcTemp,
                        (char)ain0EF.getTempUnits(),
                        tcVolts,
                        cjcTemp,
                        (char)ain0EF.getTempUnits()
                    );
                    LJM.WaitForNextInterval(
                        intervalHandle,
                        ref skippedIntervals
                    );
                    if (skippedIntervals > 0)
                    {
                        Console.WriteLine(
                            "SkippedIntervals: " + skippedIntervals
                        );
                    }
                }
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            //Close interval and all device handles.
            try
            {
                LJM.CleanInterval(intervalHandle);
                LJM.Close(handle);
            }
            catch (LJM.LJMException)
            {
                //Ignore invalid interval handle error.
                //Ignore error closing device connection.
            }

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); //Pause for user.
        }
        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }
    }
}
