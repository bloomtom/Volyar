using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace VolyExternalApiAccess
{
    public class ApiResponse<T>
    {
        public T Value { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
        public string ErrorDetails { get; private set; }

        public bool IsSuccessStatusCode
        {
            get
            {
                int status = (int)StatusCode;
                return status >= 200 && status <= 299;
            }
        }

        public ApiResponse(T value, HttpStatusCode statusCode, string errorDetails = null)
        {
            Value = value;
            StatusCode = statusCode;
            ErrorDetails = errorDetails;
        }
    }
}
