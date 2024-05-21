using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    //Digital output
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            //TO DO: IMPLEMENT
            // paket = napraviti niz od 12 byte
            // params = this.commandParameters cast read/write
            //short -> hton -> byte -> copy paket 

            //return paket
            ModbusWriteCommandParameters ModbusWrite = this.CommandParameters as ModbusWriteCommandParameters;
            byte[] request = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)ModbusWrite.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)ModbusWrite.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)ModbusWrite.Length)), 0, request, 4, 2);
            request[6] = ModbusWrite.UnitId;
            request[7] = ModbusWrite.FunctionCode;
            // Difference with read pack request 
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)ModbusWrite.OutputAddress)), 0, request, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)ModbusWrite.Value)), 0, request, 10, 2);
            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            //TO DO: IMPLEMENT
            //recnik = napraviti recnik
            //adresu = response [8 i 9] -> short -> ntoh
            //vrednost = response [10 i 11] -> short -> ntoh
            //recnik.add(PT.DO,adresa,vrednost) AO za analogni
            //return recnik
            ModbusWriteCommandParameters ModbusWrite = this.CommandParameters as ModbusWriteCommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> dic = new Dictionary<Tuple<PointType, ushort>, ushort>();

            ushort value = (ushort)(response[11] + (response[10] << 8));
            dic.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, (ushort)(ModbusWrite.OutputAddress)), value);

            return dic;
        }
    }
}