//-----------------------------------------------------------------------------
// ThermocoupleExampleInAmp.cs
//
// Demonstrates thermocouple configuration and measurement using the
// LJTick-InAmp (commonly used with the T4). If you are using a device such as
// the T7 or T8 which supports the thermocouple AIN_EF, see
// 'ThermocoupleExampleAINEF' instead.
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

namespace ThermocoupleExampleInAmp
{
    class ThermocoupleExampleInAmp
    {
        public enum TempUnits {
            DEGK = 'K',
            DEGC = 'C',
            DEGF = 'F',
        }

        public (double tcTemp, double tcVolts, double cjcTemp) getReadingsInAmp(
            int handle,
            int tcType,
            string ainChannel,
            string cjcName,
            double cjcSlope,
            double cjcOffset,
            double inAmpGain,
            double inAmpOffset,
            TempUnits tempUnits
        ){
            const int NUM_FRAMES = 2;
            string[] aNames = {ainChannel, cjcName};
            double[] aValues = new double[NUM_FRAMES];
            int errorAddress = 0;
            double tcTemp = 0, tcVolts = 0, cjcTemp = 0;

            try
            {
                //Read the InAmp output voltage (connected to AIN#) and
                //the CJC temperature.
                LJM.eReadNames(
                    handle,
                    NUM_FRAMES,
                    aNames,
                    aValues,
                    ref errorAddress
                );

                //Convert the InAmp voltage to the raw thermocouple voltage.
                tcVolts = (aValues[0] - inAmpOffset) / inAmpGain;

                //Apply scaling to CJC reading if necessary.
                //At this point the reading must be in units Kelvin.
                cjcTemp = aValues[1] * cjcSlope + cjcOffset;

                //Convert voltage reading to the thermocouple temperature.
                LJM.TCVoltsToTemp(tcType, tcVolts, cjcTemp, ref tcTemp);

                //Convert to temperature units for display.
                switch (tempUnits){
                    case TempUnits.DEGC:
                        tcTemp = tcTemp - 273.15;
                        cjcTemp = cjcTemp - 273.15;
                        break;
                    case TempUnits.DEGF:
                        tcTemp = (1.8 * tcTemp) - 459.67;
                        cjcTemp = (1.8 * cjcTemp) - 459.67;
                        break;
                    case TempUnits.DEGK:
                    default:
                        //Assume DEGK, no conversion necessary.
                        break;
                }
            }
            catch (Exception)
            {
                Console.Out.WriteLine(
                    "Exception in ThermocoupleExampleInAmp.getReadingsInAmp"
                );
                throw;
            }
            return (tcTemp, tcVolts, cjcTemp);
        }

        static void Main(string[] args)
        {
            ThermocoupleExampleInAmp tce = new ThermocoupleExampleInAmp();
            tce.performActions();
        }

        public void performActions()
        {
            int tcType;
            string ainChannel;
            string cjcName;
            double cjcSlope;
            double cjcOffset;
            double inAmpGain;
            double inAmpOffset;
            TempUnits tempUnits;
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

            try
            {
                //Open the first found LabJack.
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);  // Any device, Any connection, Any identifier
                //LJM.OpenS("T8", "ANY", "ANY", ref handle);  // T8 device, Any connection, Any identifier
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  // T7 device, Any connection, Any identifier
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  // T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

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

                //Supported TC types are:
                //    LJM_ttB (val=6001)
                //    LJM_ttE (val=6002)
                //    LJM_ttJ (val=6003)
                //    LJM_ttK (val=6004)
                //    LJM_ttN (val=6005)
                //    LJM_ttR (val=6006)
                //    LJM_ttS (val=6007)
                //    LJM_ttT (val=6008)
                //    LJM_ttC (val=6009)
                //Take a type K thermocouple measurement.
                tcType = LJM.CONSTANTS.ttK;
                //Take the thermocouple reading on AIN6.
                ainChannel = "AIN6";
                //Use the internal temp sensor for CJC.
                cjcName = "TEMPERATURE_DEVICE_K";
                cjcSlope = 1; //55.56 for LM34.
                cjcOffset = 0; //255.37 for LM34.
                inAmpGain = 51; //Assuming InAmp with x51 gain.
                inAmpOffset = 1.25; //Assuming InAmp with 1.25 V offset.
                //Output temperature readings in °C.
                tempUnits = TempUnits.DEGC;

                //Set the resolution index to the default setting (value=0).
                LJM.eWriteName(handle, ainChannel + "_RESOLUTION_INDEX", 0);

                Console.WriteLine(
                    "\nReading thermocouple temperature in a loop." +
                    " Press a key to stop.\n"
                );
                LJM.StartInterval(intervalHandle, 1000000);
                while (!Console.KeyAvailable)
                {
                    (tcTemp, tcVolts, cjcTemp) = getReadingsInAmp(
                        handle,
                        tcType,
                        ainChannel,
                        cjcName,
                        cjcSlope,
                        cjcOffset,
                        inAmpGain,
                        inAmpOffset,
                        tempUnits
                    );
                    Console.WriteLine(
                        "tcTemp: {0:N3} {1}," +
                        "\t tcVolts: {2:N6}," +
                        "\tCJTemp: {3:N3} {4}\n",
                        tcTemp,
                        (char)tempUnits,
                        tcVolts,
                        cjcTemp,
                        (char)tempUnits
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
