//
//  Copyright (c) 2017 Ricoh Company, Ltd. All Rights Reserved.
//  See LICENSE for more information.
//
using System;


namespace MtpHelper
{
    public class MtpHelperRuntimeException: Exception
    {
        public MtpHelperRuntimeException() {}
        public MtpHelperRuntimeException(String message): base(message) {}
        public MtpHelperRuntimeException(String message, Exception inner): base(message, inner) {}
    }
}
