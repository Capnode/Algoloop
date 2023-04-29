/*
 * Copyright 2018 Capnode AB
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

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Algoloop.Wpf
{
    /// <summary>
    /// Interaction logic for MarketsView.xaml
    /// </summary>
    public partial class MarketsView : UserControl
    {
        private readonly Dictionary<TextBlock, double> _cellValue = new();
        private readonly Storyboard _greenBlink = new();
        private readonly Storyboard _redBlink = new();

        public MarketsView()
        {
            InitializeComponent();

            System.Drawing.Color green = System.Drawing.Color.LightGreen;
            ColorAnimation animation = new()
            {
                BeginTime = new TimeSpan(0, 0, 0, 0, 200),
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, 500)),
                From = Color.FromArgb(green.A, green.R, green.G, green.B),
                To = Color.FromArgb(0, 0, 0, 0)
            };
            Storyboard.SetTargetProperty(animation, new PropertyPath("(TextBlock.Background).(SolidColorBrush.Color)"));
            _greenBlink.Children.Add(animation);

            System.Drawing.Color red = System.Drawing.Color.Salmon;
            animation = new ColorAnimation
            {
                BeginTime = new TimeSpan(0, 0, 0, 0, 200),
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, 500)),
                From = Color.FromArgb(red.A, red.R, red.G, red.B),
                To = Color.FromArgb(0, 0, 0, 0)
            };
            Storyboard.SetTargetProperty(animation, new PropertyPath("(TextBlock.Background).(SolidColorBrush.Color)"));
            _redBlink.Children.Add(animation);
        }

        private void TargetUpdatedBlinkCell(object sender, DataTransferEventArgs e)
        {
            if (e.TargetObject is not TextBlock tb)
                return;

            if (!_cellValue.ContainsKey(tb))
            {
                _cellValue[tb] = 0;
                tb.Background = Brushes.Transparent;
                tb.Foreground = Brushes.Black;
            }

            double prevValue = _cellValue[tb];
            string text = tb.Text.Replace((char)8722, '-'); // Convert Unicode minus
            if (!double.TryParse(text, out double currValue))
                return;

            if (prevValue < currValue)
            {
                Storyboard.SetTarget(_greenBlink, tb);
                _greenBlink.Begin();
            }
            else if (prevValue > currValue)
            {
                Storyboard.SetTarget(_redBlink, tb);
                _redBlink.Begin();
            }

            _cellValue[tb] = currValue;
        }
    }
}
