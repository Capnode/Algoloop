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

using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Algoloop.Avalonia
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            if (data is null)
                return null;

            // Simplified view locator for the Avalonia demo
            return new TextBlock { Text = "View for: " + data.GetType().Name };
        }

        public bool Match(object? data)
        {
            // For now, match any object
            return data != null;
        }
    }
}
