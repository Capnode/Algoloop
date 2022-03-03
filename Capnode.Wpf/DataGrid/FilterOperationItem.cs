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

namespace Capnode.Wpf.DataGrid
{
    public class FilterOperationItem
    {
        public Enums.FilterOperation FilterOption { get; set; }
        public string ImagePath { get; set; }
        public string Description { get; set; }
        public FilterOperationItem(Enums.FilterOperation operation, string description, string imagePath)
        {
            FilterOption = operation;
            Description = description;
            ImagePath = imagePath;
        }
        public override string ToString()
        {
            return Description;
        }
    }
}
