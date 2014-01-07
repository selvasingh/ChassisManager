﻿/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
*   Auther:  Bryankel@Microsoft.com                     *
*                                       	            *
********************************************************/

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi
{

    /// <summary>
    /// Represents the IPMI 'Set User Name' application response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Application, IpmiCommand.SetUserName)]
    class SetUserNameResponse : IpmiResponse
    {
    }
}