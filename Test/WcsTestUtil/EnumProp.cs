// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at 
// http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
// See the Apache 2 License for the specific language governing permissions and limitations under the License. 

namespace WcsTestUtil
{
    using System;
    using System.Collections;
    using System.Reflection;
    class EnumProp
    {
        public static void EnumerableObject(object classObject)
        {
            Type objectType = classObject.GetType();
            Console.WriteLine();
            Console.WriteLine(" Enumerating Objects for: {0}", objectType.Name);
            Console.WriteLine(" ===================================");
            Console.WriteLine();
            try
            {
                foreach (PropertyInfo prop in objectType.GetProperties(BindingFlags.Public 
                | BindingFlags.Instance
                | BindingFlags.DeclaredOnly))
                {
                    // property name
                    string propertyName = prop.Name;
                    // property type
                    Type propertyType = prop.PropertyType;
                    // check if the type is a collection (Dictionary, List).
                    if (typeof(ICollection).IsAssignableFrom(propertyType))
                    {
                        Console.WriteLine("     Property: {0}", propertyName);
                        // Get enumerable values.
                        IEnumerable values = (IEnumerable)prop.GetValue(classObject, null);
                        // Enumerate all sub-values of the collection
                        foreach (var item in values)
                        {
                            Console.WriteLine("                       Value: {0}", item.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("     Property: {0}", propertyName);
                        string value = prop.GetValue(classObject, null).ToString();
                        Console.WriteLine("                       Value: {0}", value);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine(" ===================================");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("===================================");
                Console.WriteLine();
                Console.WriteLine("Error Enumerating Properties for {0}", objectType.Name);
                Console.WriteLine(ex.ToString());
                Console.WriteLine();
                Console.WriteLine("===================================");
                Console.WriteLine();
            }
        
        }
    }
}
