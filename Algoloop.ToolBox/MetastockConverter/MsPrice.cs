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
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Algoloop
{
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public class MsPrice
  {
    private uint _iDate;
    private uint _iOpen;
    private uint _iHigh;
    private uint _iLow;
    private uint _iClose;
    private uint _iVolume;
    private uint _iOpenInterest;

    private DateTime _date = DateTime.MinValue;
    private float _open = float.NaN;
    private float _high = float.NaN;
    private float _low = float.NaN;
    private float _close = float.NaN;
    private float _volume = float.NaN;
    private float _openInterest = float.NaN;

    byte _fields;
    MsPrice _previous;

    public MsPrice(byte fields, MsPrice previous)
    {
      Debug.Assert(fields >= 5 && fields <= 7, "fields >= 5 && fields <= 7");
      _fields = fields;
      _previous = previous;
    }

    internal long Read(BinaryReader br)
    {
      _iDate = br.ReadUInt32();
      if (_fields >= 6)
        _iOpen = br.ReadUInt32();
      else
        _iOpen = 0;
      _iHigh = br.ReadUInt32();
      _iLow = br.ReadUInt32();
      _iClose = br.ReadUInt32();
      _iVolume = br.ReadUInt32();
      if (_fields >= 7)
        _iOpenInterest = br.ReadUInt32();
      else
        _iOpenInterest = 0;
      return _fields * sizeof (UInt32);
    }

    public byte Fields
    {
      get {return _fields;}
    }

    public uint Count
    {
      // this is not correct
      get { return _iDate >> 16; }
    }

    public DateTime Date
    {
      get 
      {
        if (_date == DateTime.MinValue)
        {
          int yyyy = 0;
          int mm = 0;
          int dd = 0;
          int yyymmdd = (int)Msbin2Ieee(_iDate);
          if (yyymmdd != 0)
          {
            yyyy = yyymmdd / 10000 + 1900;
            mm = (yyymmdd % 10000) / 100;
            dd = yyymmdd % 100;
          }
          _date = new DateTime(yyyy, mm, dd);
        }
        return _date;
      }
    }

    public float Open
    {
      get 
      {
        if (float.IsNaN(_open))
          _open = Msbin2Ieee(_iOpen);
        return _open;
      }
    }

    public float High
    {
      get
      {
        if (float.IsNaN(_high))
          _high = Msbin2Ieee(_iHigh);
        return _high;
      }
    }

    public float Low
    {
      get
      {
        if (float.IsNaN(_low))
          _low = Msbin2Ieee(_iLow);
        return _low;
      }
    }

    public float Close
    {
      get
      {
        if (float.IsNaN(_close))
          _close = Msbin2Ieee(_iClose);
        return _close;
      }
    }

    public float Volume
    {
      get
      {
        if (float.IsNaN(_volume))
          _volume = Msbin2Ieee(_iVolume);
        return _volume;
      }
    }

    public float OpenInterest
    {
      get
      {
        if (float.IsNaN(_openInterest))
          _openInterest = Msbin2Ieee(_iOpenInterest);
        return _openInterest;
      }
    }

    public float CloseDiff
    {
      get
      {
        if (_previous == null)
          return 0;
        else
          return Close - _previous.Close;
      }
    }

    public float ClosePercent
    {
      get
      {
        if (_previous == null)
          return 0;
        else
        {
          float close0 = _previous.Close;
          return (Close - close0) / close0;
        }
      }
    }

    [System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit)]
    struct union
    {
      [System.Runtime.InteropServices.FieldOffset(0)]
      internal float a;
      [System.Runtime.InteropServices.FieldOffset(0)]
      internal uint b;
    };

    float Msbin2Ieee(uint msbin)
    {
      // Microsoft Basic floating point format to IEEE floating point format
      union c;
      c.a = 0;
      if (msbin != 0) 
      {
        uint mantissa;
        uint exponent;
        c.b = msbin;
        mantissa = (ushort)(c.b >> 16);
	      exponent = (mantissa & (ushort)0xff00) - (ushort)0x0200;
        if ((exponent & 0x8000) != (mantissa & 0x8000))
          return 0;		/* exponent overflow */
	      mantissa = mantissa & 0x7f | (mantissa << 8) & 0x8000;		/* move sign */
	      mantissa |= exponent >> 1;
	      c.b = c.b & 0xffff | mantissa << 16;
      }
      return c.a;
    }

    public override string ToString()
    {
      return string.Format("MsPrice fields={0} date={1} open={2} high={3} low={4} close={5} volume={6} oi={7}", 
        Fields, Date.ToShortDateString(), Open, High, Low, Close, Volume, OpenInterest);
    }
  }
}
