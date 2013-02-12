﻿#region License
//
// Copyright (c) 2008-2012, DoLittle Studios AS and Komplett ASA
//
// Licensed under the Microsoft Permissive License (Ms-PL), Version 1.1 (the "License")
// With one exception :
//   Commercial libraries that is based partly or fully on Bifrost and is sold commercially, 
//   must obtain a commercial license.
//
// You may not use this file except in compliance with the License.
// You may obtain a copy of the license at 
//
//   http://bifrost.codeplex.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion
namespace Bifrost.CodeGeneration.JavaScript
{
    /// <summary>
    /// Represents a variant assignment
    /// </summary>
    public class VariantAssignment : Assignment
    {
        /// <summary>
        /// Initializes a new instance of <see cref="VariantAssignment"/>
        /// </summary>
        /// <param name="name">Name of the variant</param>
        /// <param name="value"><see cref="ILanguageElement">value</see> to assign</param>
        public VariantAssignment(string name, ILanguageElement value = null)
            : base(name, value)
        {
        }

#pragma warning disable 1591
        public override void Write(ICodeWriter writer)
        {
            writer.WriteWithIndentation("var {0} = ", Name);
            if( Value != null ) Value.Write(writer);
            writer.Write(";");
            writer.Newline();
        }
#pragma warning restore 1591
    }
}