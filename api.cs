/*
Call the api client like this:
	var client = new ApiClient<SEnvelope>("https://baseurl.com/api/v1");
	//you would overload and add an auth_token param here 
 	client.GetDtoAsync("envelopes", "object_id", (response) => //callback
                                                          {
                                                              this.SEnvelope = response.Data;//should be an envelope from the server
                                                          });
                                                          
Integration / Unit Test Async calls like this:
 	[Test]
        public void can_get_envelope_from_api()
        {
            var client = new ApiClient<SEnvelope>("https://baseurl.com/api/v1");
            var resetEvent = new ManualResetEvent(false); 
            client.GetDtoAsync("envelopes", "object_id", (response) =>
                                                          {
                                                              Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                                                              //other assertions here
                                                              resetEvent.Set();
                                                          });
            resetEvent.WaitOne();
        }
 
*/



namespace foo.bar 
{
	public class Dto
	{
		public String ObjectId{ get; set; }
		/* Other common properties here */
	}

	public class ApiClient<T> where T : Dto, new()
	{
		private const String JsonContentType = "application/json; charset=utf-8";
		private readonly string serviceBaseUrl;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="serviceBaseUrl">e.g. https://baseurl.com/api/v1</param>
		public ApiClient (String serviceBaseUrl)
		{
			this.serviceBaseUrl = serviceBaseUrl;
		}        

		public HttpStatusCode PostDtoSync (T dto, String resource)
		{

			var client = new RestClient (serviceBaseUrl);

			var request = CreateJsonRequest (resource);

			request.AddBody (dto);

			request.Method = Method.POST;
			var response = client.Execute (request);
			return response.StatusCode;
		}

		public void PostDtoAsync (T dto, string resource, Action<IRestResponse> callback)
		{
			var client = new RestClient (serviceBaseUrl);
			var request = CreateJsonRequest (resource);

			request.AddBody (dto);
			request.Method = Method.POST;
			client.ExecuteAsync (request, callback);
		}

		public HttpStatusCode DeleteDto (String resource, String userId, String itemId)
		{
			var client = new RestClient (serviceBaseUrl);
			RestRequest request = CreateDeleteRequest (resource, userId, itemId);

			var response = client.Execute (request);
			return response.StatusCode;
		}

		public HttpStatusCode DeleteDto (String resource, String userId, long itemId)
		{
			return DeleteDto (resource, userId, itemId.ToString ());
		}

		public void DeleteDtoAsync (String resource, String userId, long itemId, Action<IRestResponse> callback)
		{
			DeleteDtoAsync (resource, userId, itemId.ToString (), callback);
		}

		public void DeleteDtoAsync (String resource, String userId, String itemId, Action<IRestResponse> callback)
		{
			var client = new RestClient (serviceBaseUrl);
			RestRequest request = CreateDeleteRequest (resource, userId, itemId);

			client.ExecuteAsync (request, callback);
		}

		public void GetDtoAsync (String resource, String id, Action<IRestResponse> callback)
		{
			var client = new RestClient (serviceBaseUrl);
			var request = CreateJsonRequest (resource);

			request.AddParameter ("id", id, ParameterType.GetOrPost);

			client.ExecuteAsync (request, callback);
		}

		public IList<T> GetDtoList (string resource, string userId)
		{
			var client = new RestClient (serviceBaseUrl);
			var request = CreateJsonRequest (resource);
			request.AddParameter ("id", userId, ParameterType.GetOrPost);

			try{ 
				var response = client.Execute<List<T>> (request);
				return response.Data;
			}catch(Exception ex){
				Console.Out.WriteLine (ex.ToString());
				return null;
			}

		}

		public void GetDtoListAsync (string resource,
		                             string userId,
		                             Action<IRestResponse<List<T>>> callback,
		                             IDictionary<String, object> parameters = null)
		{
			var client = new RestClient (serviceBaseUrl);

			var request = CreateJsonRequest (resource);
			request.AddParameter ("id", userId, ParameterType.GetOrPost);
			if (parameters != null && parameters.Keys.Count > 0) {

				foreach (var key in parameters.Keys) {
					request.AddParameter (key, parameters[key], ParameterType.GetOrPost);
				}

			}
			client.ExecuteAsync (request, callback);
		}

		public void UploadImage (byte[] byteArray, string imageName, string fileExtension, string userId, Action callback)
		{
			var mem = new MemoryStream (byteArray) { Position = 0 };
			mem.Seek (0, SeekOrigin.Begin);

			var request = WebRequest.Create (serviceBaseUrl + "image");
			request.Headers.Add ("imageName", imageName);
			request.Headers.Add ("fileExtension", fileExtension);
			request.Headers.Add ("userId", userId);

			request.Method = "POST";
			var requestState = new RequestState
            {
                Request = request,
                WriteStream = mem,
				Callback = callback
            };
			//no waiting or response retrieval here. just fire and forget
			request.BeginGetRequestStream (RequestCallback, requestState);
		}

		private static void RequestCallback (IAsyncResult ar)
		{
			var requestState = (RequestState)ar.AsyncState;
			var responseStream = requestState.Request.EndGetRequestStream (ar);
			requestState.WriteStream.CopyTo (responseStream);
			requestState.WriteStream.Flush ();
			responseStream.Close ();

			requestState.Request.BeginGetResponse ((a) =>
			{
				Console.Out.WriteLine ("Uploaded image");
				if (requestState.Callback != null) {
					requestState.Callback ();
				}
			}, null);
		} 

		private RestRequest CreateDeleteRequest (string resource, string userId, string itemId)
		{
			var request = new RestRequest (resource, Method.DELETE);
			request.AddParameter ("userId", userId, ParameterType.GetOrPost);
			request.AddParameter ("id", itemId, ParameterType.GetOrPost);
			return request;
		}

		private IRestRequest CreateJsonRequest (String resource)
		{
			var request = new RestRequest (resource)
			{
				RequestFormat = DataFormat.Json,
				JsonSerializer =
				{
					ContentType = JsonContentType
				},
				// You can set all your defaults here, e.g. Date format:
				//DateFormat = "ISO 8601"
			};

			return request;
		}
	}

	// The RequestState class passes data across async calls.
	public class RequestState
	{
		public Stream WriteStream;
		public WebRequest Request;
		public Stream ResponseStream;
		public Action Callback;
		public RequestState ()
		{
			Request = null;
			ResponseStream = null;
			WriteStream = null;
			Callback = null;
		}
	}
}
