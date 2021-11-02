using System;
using System.Text;
using System.Diagnostics;
using Lawicel;
using static Lawicel.CANUSB;

namespace VisualServoCore.Communication
{
    public class CanHandlerForEv : ICommunication<string>
    {

        // ------ Fields ------ //

        // Tire-Angle Order (int16, LSB=0.1deg)
        private const uint _sendSteerId = 0x0000062F;
        private const uint _recvSteerId = 0x0000072F;
        private readonly uint _handle;


        // ------ Constructors ------ //

        public CanHandlerForEv()
        {
            canusb_getFirstAdapter(new StringBuilder(32), 32);
            //
            //	To test a diffrent Mask/Filter, change argument 3 & 4 to these instead:
            //
            //	0x1FBD1FFD Acceptance code for accepting only 0x5E8 and 0x7F8
            //	0x1F001F00 Acceptance mask for accepting only 0x5E8 and 0x7F8
            //

            // Open channel at 500 kbps, no filter
            _handle = canusb_Open(
                IntPtr.Zero, 
                CAN_BAUD_500K, 
                CANUSB_ACCEPTANCE_CODE_ALL, 
                CANUSB_ACCEPTANCE_MASK_ALL,
                CANUSB_FLAG_TIMESTAMP
            );

            if (_handle < 1)
                Debug.WriteLine("failed to open CAN connection!");
        }


        // ------ Methods ------ //

        public bool Send(string steer)
        {
            var shortSteer = short.Parse(steer);
            var sendMsg = new CANMsg();
            var steer_order = new byte[8];
            shortSteer = (short)(shortSteer * 10);
            sendMsg.id = 0x0000062F;
            sendMsg.len = 8;
            steer_order[0] = 0x1F;
            steer_order[1] = 0x0;
            steer_order[3] = (byte)(shortSteer >> 8);
            steer_order[2] = (byte)(shortSteer);
            for (int i = 7; i >= 0; i--) // Data Frame
            {
                sendMsg.data = (sendMsg.data << 8) | steer_order[i];
            }

            // w_msg.flags = CANMSG_EXTENDED;
            int spary_rv = canusb_Write(_handle, ref sendMsg);
            if (spary_rv is ERROR_CANUSB_OK)
            {
                // Message send successfully;
                return true;
            }
            else if (spary_rv is ERROR_CANUSB_TX_FIFO_FULL)
            {
                Debug.WriteLine("FIFO full. Can't send message.");
                return false;
            }
            else
            {
                Debug.WriteLine("failed to send message.");
                return false;
            }
        }

        public string Receive()
        {
            var rcvMsg = new CANMsg();
            var steer_msg = new byte[8];
            //var rv = canusb_ReadFirst(_handle, Read_can_id, can_flag, out r_msg);
            while (true)
            {
                rcvMsg.data = 0;
                var rv = canusb_Read(_handle, out rcvMsg);
                if (rv is ERROR_CANUSB_OK && rcvMsg.id is _recvSteerId)
                {
                    for (int i = 0; i < rcvMsg.len; i++) // Data Frame
                    {
                        steer_msg[i] = (byte)(rcvMsg.data >> (i * 8));
                    }
                    short msg = steer_msg[2];
                    msg = (short)((short)(msg << 8) | (short)steer_msg[1]);
                    return $"{msg}";
                }
            }
        }

        public void Dispose()
        {
            if (CANUSB.canusb_Close(_handle) is not CANUSB.ERROR_CANUSB_OK)
                Debug.WriteLine("failed to close CAN connection!");
        }

    }
}
