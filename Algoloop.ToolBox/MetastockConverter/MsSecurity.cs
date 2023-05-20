//  Copyright (C) 2012 Capnode AB

//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Algoloop
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MsSecurity : IComparable
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]

        // Header record
        ushort _numFiles; // Number of Files	UW	1	2	Number of files in MASTER
        ushort _nextFile; // Next file	UW	3	2	Number to assign to next new Fn file

        // Standard record
        byte _fileNum; // File Number	UB	1	1	The n value of file names Fn
        byte[] _type; // Type	UW	2	2	Computrac file type = $e0//  Copyright (C) 2012 Capnode AB

        //  This program is free software: you can redistribute it and/or modify
        //  it under the terms of the GNU General Public License as published by
        //  the Free Software Foundation, either version 2 of the License, or
        //  (at your option) any later version.

        //  This program is distributed in the hope that it will be useful,
        //  but WITHOUT ANY WARRANTY; without even the implied warranty of
        //  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
        //  GNU General Public License for more details.

        //  You should have received a copy of the GNU General Public License
        //  along with this program.  If not, see <http://www.gnu.org/licenses/>.


        byte _length; // Length	UB	4	1	Record length
        byte _fields; // Fields	UB	5	1	Fields per record in Fn.dat file
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        byte[] _name; // Security	A	8	16	Security name in ASCII blank padded
        byte _vflag; // Vflag	A	25	1	$00 Version 2.8 flag
        uint _firstDate; // First Date	MBF	26	4	First date in Fn.dat file
        uint _lastDate; // Last Date	MBF	30	4	Last date in Fn.dat file
        byte _period; // Period	A	34	1	Time period for records: IDWMQY
        ushort _time; // Time	UW	35	2	Intraday time Base, $00 $00
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        byte[] _symbol; // Symbol	A	12	14	Stock symbol
        byte _autorun; // AutoRun	A	52	1	ASCII ‘*’ for autorun

        private readonly string _root;

        internal MsSecurity(string root)
        {
            _root = root;
        }

        public int CompareTo(object obj)
        {
            if (obj is not MsSecurity security) throw new ArgumentException("object is not a MsSecurity");
            return Name.CompareTo(security.Name);
        }

        internal long ReadHeader(BinaryReader br)
        {
            _numFiles = br.ReadUInt16();
            _nextFile = br.ReadUInt16();
            br.ReadBytes(49);
            return 53;
        }

        internal long Read(BinaryReader br)
        {
            _fileNum = br.ReadByte();
            _type = br.ReadBytes(2);
            _length = br.ReadByte();
            _fields = br.ReadByte();
            br.ReadBytes(2); // reserved1
            _name = br.ReadBytes(16);
            br.ReadByte(); // reserved2
            _vflag = br.ReadByte();
            _firstDate = br.ReadUInt32();
            _lastDate = br.ReadUInt32();
            _period = br.ReadByte();
            _time = br.ReadUInt16();
            _symbol = br.ReadBytes(14);
            br.ReadByte(); // reserved3
            _autorun = br.ReadByte();
            br.ReadByte(); // reserved4
            return 53;
        }

        unsafe public string Symbol
        {
            get
            {
                int len = 0;
                foreach (byte c in _symbol)
                {
                    if (c == 0)
                    {
                        break;
                    }
                    else
                    {
                        len++;
                    }
                }

                string str;
                // Instruct the Garbage Collector not to move the memory
                fixed (sbyte* sp = (sbyte[])(Array)_symbol)
                {
                    str = new String(sp, 0, len);
                }

                return str.TrimEnd();
            }
        }

        unsafe public string Name
        {
            get
            {
                int len = 0;
                foreach (byte c in _name)
                {
                    if (c == 0)
                    {
                        break;
                    }
                    else
                    {
                        len++;
                    }
                }

                string str;
                // Instruct the Garbage Collector not to move the memory
                fixed (sbyte* sp = (sbyte[])(Array)_name)
                {
                    str = new String(sp, 0, len);
                }
                return str.TrimEnd();
            }
        }

        public string Filename
        {
            get
            {
                return _root + @"\" + string.Format("F{0}", _fileNum);
            }
        }

        public byte Fields
        {
            get
            {
                return _fields;
            }
        }

        public override string ToString()
        {
            return string.Format("MsSecurity symbol={0} name={1}", Symbol, Name);
        }

        public List<MsPrice> GetPriceList()
        {
            List<MsPrice> prices = new();
            string datFile = Filename + ".dat";
            if (File.Exists(datFile))
            {
                FileStream fs = new(datFile, FileMode.Open, FileAccess.Read);
                BinaryReader br = new(fs);
                try
                {
                    MsPrice price = new(Fields, null); // Header
                    long size = fs.Length;
                    long pos = 0;
                    pos += price.Read(br);
                    price = null;
                    while (pos < size)
                    {
                        price = new MsPrice(Fields, price);
                        pos += price.Read(br);
                        prices.Add(price);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    br.Close();
                }
            }
            return prices;
        }
    }
}
