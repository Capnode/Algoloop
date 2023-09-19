/*
 * Copyright 2019 Capnode AB
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Algoloop.Algorithm.CSharp.Model;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;

namespace Algoloop.Algorithm.CSharp.Signal
{
    internal class TideSignal : ISignal
    {
        private readonly Resolution _resolution;
        private readonly InsightDirection _direction;
        private readonly int _open;

        public TideSignal(Resolution resolution, InsightDirection direction, int open)
        {
            _resolution = resolution;
            _direction = direction;
            _open = open;
        }

        public float Update(QCAlgorithm algorithm, BaseData bar)
        {
            switch (_resolution)
            {
                case Resolution.Daily:
                    //                    if (bar.Time.DayOfWeek.Equals((DayOfWeek)_trigger))
                    if (bar.Time.Day.Equals(_open))
                    {
                        return (float)_direction;
                    }
                    break;

                case Resolution.Hour:
                    if (bar.Time.Hour.Equals(_open))
                    {
                        return (float)_direction;
                    }
                    break;

                case Resolution.Minute:
                    if (bar.Time.Minute.Equals(_open))
                    {
                        return (float)_direction;
                    }
                    break;

                case Resolution.Second:
                    if (bar.Time.Second.Equals(_open))
                    {
                        return (float)_direction;
                    }
                    break;
            }

            return 0;
        }
    }
}
