/*   Syncrio License
 *   
 *   Copyright © 2016 Caleb Huyck
 *   
 *   This file is part of Syncrio.
 *   
 *   Syncrio is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *   
 *   Syncrio is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *   
 *   You should have received a copy of the GNU General Public License
 *   along with Syncrio.  If not, see <http://www.gnu.org/licenses/>.
 */

/*   DarkMultiPlayer License
 * 
 *   Copyright (c) 2014-2016 Christopher Andrews, Alexandre Oliveira, Joshua Blake, William Donaldson.
 *
 *   Permission is hereby granted, free of charge, to any person obtaining a copy
 *   of this software and associated documentation files (the "Software"), to deal
 *   in the Software without restriction, including without limitation the rights
 *   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *   copies of the Software, and to permit persons to whom the Software is
 *   furnished to do so, subject to the following conditions:
 *
 *   The above copyright notice and this permission notice shall be included in all
 *   copies or substantial portions of the Software.
 *
 *   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *   SOFTWARE.
 */


using System;
using UnityEngine;

namespace SyncrioClientSide
{
    public class SyncrioGuiUtil
    {
        public const ControlTypes BLOCK_ALL_CONTROLS = ControlTypes.ALLBUTCAMERAS ^ ControlTypes.MAP ^ ControlTypes.PAUSE ^ ControlTypes.APPLAUNCHER_BUTTONS ^ ControlTypes.VESSEL_SWITCHING ^ ControlTypes.GUI;
        public static Rect PreventOffscreenWindow(Rect inputRect)
        {
            //Let the user drag 3/4 of the window sideways off the screen
            float xMin = 0 - ((3 / 4f) * inputRect.width);
            float xMax = Screen.width - ((1 / 4f) * inputRect.width);
            //Don't let the title bar move above the top of the screen
            float yMin = 0;
            //Don't let the title bar move below the bottom of the screen
            float yMax = Screen.height - 20;
            if (inputRect.x < xMin)
            {
                inputRect.x = xMin;
            }
            if (inputRect.x  > xMax)
            {
                inputRect.x = xMax;
            }
            if (inputRect.y < yMin)
            {
                inputRect.y = yMin;
            }
            if (inputRect.y > yMax)
            {
                inputRect.y = yMax;
            }
            return inputRect;
        }
    }
}

