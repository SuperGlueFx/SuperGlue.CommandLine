using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SuperGlue.Web
{
    internal static class WebEnvironmentExtensions
    {
        public static WebRequest GetRequest(this IDictionary<string, object> environment)
        {
            return new WebRequest(environment);
        }

        public static WebResponse GetResponse(this IDictionary<string, object> environment)
        {
            return new WebResponse(environment);
        }

        internal static class OwinConstants
        {
            // http://owin.org/spec/owin-1.0.0.html

            public const string RequestScheme = "owin.RequestScheme";
            public const string RequestMethod = "owin.RequestMethod";
            public const string RequestPathBase = "owin.RequestPathBase";
            public const string RequestPath = "owin.RequestPath";
            public const string RequestQueryString = "owin.RequestQueryString";
            public const string RequestProtocol = "owin.RequestProtocol";
            public const string RequestHeaders = "owin.RequestHeaders";
            public const string RequestBody = "owin.RequestBody";
            public const string CallCancelled = "owin.CallCancelled";

            // http://owin.org/spec/owin-1.0.0.html

            public const string ResponseStatusCode = "owin.ResponseStatusCode";
            public const string ResponseReasonPhrase = "owin.ResponseReasonPhrase";
            public const string ResponseProtocol = "owin.ResponseProtocol";
            public const string ResponseHeaders = "owin.ResponseHeaders";
            public const string ResponseBody = "owin.ResponseBody";

            internal static class CommonKeys
            {
                public const string ClientCertificate = "ssl.ClientCertificate";
                public const string RemoteIpAddress = "server.RemoteIpAddress";
                public const string RemotePort = "server.RemotePort";
                public const string LocalIpAddress = "server.LocalIpAddress";
                public const string LocalPort = "server.LocalPort";
                public const string IsLocal = "server.IsLocal";
                public const string TraceOutput = "host.TraceOutput";
                public const string Addresses = "host.Addresses";
                public const string AppName = "host.AppName";
                public const string Capabilities = "server.Capabilities";
                public const string OnSendingHeaders = "server.OnSendingHeaders";
                public const string OnAppDisposing = "host.OnAppDisposing";
                public const string Scheme = "scheme";
                public const string Host = "host";
                public const string Port = "port";
                public const string Path = "path";
            }
        }

        internal static class HeadersConstants
        {
            internal const string ContentType = "Content-Type";
            internal const string CacheControl = "Cache-Control";
            internal const string MediaType = "Media-Type";
            internal const string Accept = "Accept";
            internal const string Host = "Host";
            internal const string ETag = "ETag";
            internal const string Location = "Location";
            internal const string ContentLength = "Content-Length";
            internal const string SetCookie = "Set-Cookie";
            internal const string Expires = "Expires";
            internal const string Pragma = "Pragma";
            internal const string Vary = "Vary";
        }

        internal class WebRequest
        {
            private readonly IDictionary<string, object> _environment;

            public WebRequest(IDictionary<string, object> environment)
            {
                _environment = environment;
            }

            public string Method => _environment.Get<string>(OwinConstants.RequestMethod);
            public string Scheme => _environment.Get<string>(OwinConstants.RequestScheme);
            public bool IsSecure => string.Equals(Scheme, "HTTPS", StringComparison.OrdinalIgnoreCase);

            public string Host
            {
                get
                {
                    var host = Headers.GetHeader("Host");
                    if (!string.IsNullOrWhiteSpace(host))
                    {
                        return host;
                    }

                    var localIpAddress = LocalIpAddress ?? "localhost";
                    var localPort = _environment.Get<string>(OwinConstants.CommonKeys.LocalPort);
                    return string.IsNullOrWhiteSpace(localPort) ? localIpAddress : (localIpAddress + ":" + localPort);
                }
            }

            public string PathBase => _environment.Get<string>(OwinConstants.RequestPathBase);
            public string Path => _environment.Get<string>(OwinConstants.RequestPath);
            public string QueryString => _environment.Get<string>(OwinConstants.RequestQueryString);
            public ReadableStringCollection Query => new ReadableStringCollection(GetQuery());
            public Uri Uri => new Uri(Scheme + Uri.SchemeDelimiter + Host + PathBase + Path + (string.IsNullOrEmpty(QueryString) ? "" : "?" + QueryString));
            public string Protocol => _environment.Get<string>(OwinConstants.RequestProtocol);
            public RequestHeaders Headers => new RequestHeaders(new ReadOnlyDictionary<string, string[]>(_environment.Get<IDictionary<string, string[]>>(OwinConstants.RequestHeaders, new Dictionary<string, string[]>())));
            public RequestCookieCollection Cookies => new RequestCookieCollection(GetCookies());
            public Stream Body => _environment.Get<Stream>(OwinConstants.RequestBody);

            public CancellationToken CallCancelled
            {
                get { return _environment.Get<CancellationToken>(OwinConstants.CallCancelled); }
                set { Set(OwinConstants.CallCancelled, value); }
            }
            public string LocalIpAddress => _environment.Get<string>(OwinConstants.CommonKeys.LocalIpAddress);

            public async Task<ReadableStringCollection> ReadForm()
            {
                return (await GetForm().ConfigureAwait(false)).Form;
            }

            public async Task<IEnumerable<HttpFile>> ReadFiles()
            {
                return (await GetForm().ConfigureAwait(false)).Files;
            }

            public int? LocalPort
            {
                get
                {
                    int value;
                    if (int.TryParse(_environment.Get<string>(OwinConstants.CommonKeys.LocalPort), out value))
                        return value;

                    return null;
                }
            }

            public string RemoteIpAddress => _environment.Get<string>(OwinConstants.CommonKeys.RemoteIpAddress);

            public int? RemotePort
            {
                get
                {
                    int value;
                    if (int.TryParse(_environment.Get<string>(OwinConstants.CommonKeys.RemotePort), out value))
                        return value;

                    return null;
                }
            }

            private static readonly char[] SemicolonAndComma = { ';', ',' };

            private IDictionary<string, string> GetCookies()
            {
                var cookies = new Dictionary<string, string>();

                var text = Headers.GetHeader("Cookie") ?? "";

                ParseDelimited(text, SemicolonAndComma, AddCookieCallback, cookies);

                return cookies;
            }

            private static readonly Action<string, string, object> AddCookieCallback = (name, value, state) =>
            {
                var dictionary = (IDictionary<string, string>)state;
                if (!dictionary.ContainsKey(name))
                {
                    dictionary.Add(name, value);
                }
            };

            private static void ParseDelimited(string text, char[] delimiters, Action<string, string, object> callback, object state)
            {
                var textLength = text.Length;
                var equalIndex = text.IndexOf('=');
                if (equalIndex == -1)
                {
                    equalIndex = textLength;
                }
                var scanIndex = 0;
                while (scanIndex < textLength)
                {
                    var delimiterIndex = text.IndexOfAny(delimiters, scanIndex);
                    if (delimiterIndex == -1)
                    {
                        delimiterIndex = textLength;
                    }
                    if (equalIndex < delimiterIndex)
                    {
                        while (scanIndex != equalIndex && char.IsWhiteSpace(text[scanIndex]))
                        {
                            ++scanIndex;
                        }
                        var name = text.Substring(scanIndex, equalIndex - scanIndex);
                        var value = text.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);
                        callback(
                            Uri.UnescapeDataString(name.Replace('+', ' ')),
                            Uri.UnescapeDataString(value.Replace('+', ' ')),
                            state);
                        equalIndex = text.IndexOf('=', delimiterIndex);

                        if (equalIndex == -1)
                        {
                            equalIndex = textLength;
                        }
                    }
                    scanIndex = delimiterIndex + 1;
                }
            }

            private void Set<T>(string key, T value)
            {
                _environment[key] = value;
            }

            private static readonly Action<string, string, object> AppendItemCallback = (name, value, state) =>
            {
                var dictionary = (IDictionary<string, List<String>>)state;

                List<string> existing;
                if (!dictionary.TryGetValue(name, out existing))
                    dictionary.Add(name, new List<string>(1) { value });
                else
                    existing.Add(value);
            };

            private async Task<FormData> GetForm()
            {
                var formData = _environment.Get<FormData>("SuperGlue.Owin.Form#data");

                if (formData != null)
                    return formData;

                var form = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(Headers.ContentType))
                {
                    formData = new FormData(new ReadableStringCollection(form.ToDictionary(x => x.Key, x => x.Value.ToArray())), new List<HttpFile>());
                    Set("SuperGlue.Owin.Form#data", formData);
                    return formData;
                }

                var contentType = Headers.ContentType;
                var mimeType = contentType.Split(';').First();
                if (mimeType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                {
                    var reader = new StreamReader(Body, Encoding.UTF8, true, 4 * 1024, true);

                    var accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                    ParseDelimited(await reader.ReadToEndAsync().ConfigureAwait(false), new[] { '&' }, AppendItemCallback, accumulator);

                    foreach (var kv in accumulator)
                        form.Add(kv.Key, kv.Value);
                }

                if (!mimeType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {
                    formData = new FormData(new ReadableStringCollection(form.ToDictionary(x => x.Key, x => x.Value.ToArray())), new List<HttpFile>());
                    Set("SuperGlue.Owin.Form#data", formData);
                    return formData;
                }

                var boundary = Regex.Match(contentType, @"boundary=(?<token>[^\n\; ]*)").Groups["token"].Value;
                var multipart = new HttpMultipart(Body, boundary);

                var files = new List<HttpFile>();

                foreach (var httpMultipartBoundary in multipart.GetBoundaries())
                {
                    if (string.IsNullOrEmpty(httpMultipartBoundary.Filename))
                    {
                        var reader = new StreamReader(httpMultipartBoundary.Value);

                        if (!form.ContainsKey(httpMultipartBoundary.Name))
                            form[httpMultipartBoundary.Name] = new List<string>();

                        form[httpMultipartBoundary.Name].Add(reader.ReadToEnd());
                    }
                    else
                    {
                        files.Add(new HttpFile(
                                           httpMultipartBoundary.ContentType,
                                           httpMultipartBoundary.Filename,
                                           httpMultipartBoundary.Value
                                           ));
                    }
                }

                formData = new FormData(new ReadableStringCollection(form.ToDictionary(x => x.Key, x => x.Value.ToArray())), files);
                Set("SuperGlue.Owin.Form#data", formData);
                return formData;
            }

            private static readonly char[] AmpersandAndSemicolon = { '&', ';' };

            private IDictionary<string, string[]> GetQuery()
            {
                var query = _environment.Get<IDictionary<string, string[]>>("SuperGlue.Owin.Query#dictionary");
                if (query == null)
                {
                    query = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                    Set("SuperGlue.Owin.Query#dictionary", query);
                }

                var text = QueryString;
                if (_environment.Get<string>("SuperGlue.Owin.Query#text") == text)
                    return query;

                query.Clear();
                var accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                ParseDelimited(text, AmpersandAndSemicolon, AppendItemCallback, accumulator);
                foreach (var kv in accumulator)
                    query.Add(kv.Key, kv.Value.ToArray());

                Set("SuperGlue.Owin.Query#text", text);

                return query;
            }

            public class RequestHeaders
            {
                public RequestHeaders(IReadOnlyDictionary<string, string[]> rawHeaders)
                {
                    RawHeaders = rawHeaders;
                }

                public IReadOnlyDictionary<string, string[]> RawHeaders { get; }
                public string ContentType => GetHeader(HeadersConstants.ContentType);
                public string MediaType => GetHeader(HeadersConstants.MediaType);
                public string Accept => GetHeader(HeadersConstants.Accept);

                public string GetHeader(string key)
                {
                    var values = GetHeaderUnmodified(key);
                    return values == null ? null : string.Join(",", values);
                }

                private string[] GetHeaderUnmodified(string key)
                {
                    if (RawHeaders == null)
                        throw new ArgumentNullException("headers");

                    string[] values;
                    return RawHeaders.TryGetValue(key, out values) ? values : null;
                }
            }

            public class RequestCookieCollection : IEnumerable<KeyValuePair<string, string>>
            {
                public RequestCookieCollection(IDictionary<string, string> store)
                {
                    if (store == null)
                        throw new ArgumentNullException(nameof(store));

                    Store = store;
                }

                private IDictionary<string, string> Store { get; }

                public string this[string key]
                {
                    get
                    {
                        string value;
                        Store.TryGetValue(key, out value);
                        return value;
                    }
                }

                public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
                {
                    return Store.GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }

            public class HttpFile
            {
                public HttpFile(string contentType, string name, Stream value)
                {
                    ContentType = contentType;
                    Name = name;
                    Value = value;
                }

                public string ContentType { get; private set; }
                public string Name { get; private set; }
                public Stream Value { get; private set; }
            }

            public class HttpMultipart
            {
                private const byte Lf = (byte)'\n';
                private readonly HttpMultipartBuffer _readBuffer;
                private readonly Stream _requestStream;

                public HttpMultipart(Stream requestStream, string boundary)
                {
                    _requestStream = requestStream;
                    var boundaryAsBytes = GetBoundaryAsBytes(boundary);
                    _readBuffer = new HttpMultipartBuffer(boundaryAsBytes);
                }

                public IEnumerable<HttpMultipartBoundary> GetBoundaries()
                {
                    return
                        (from boundaryStream in GetBoundarySubStreams()
                         select new HttpMultipartBoundary(boundaryStream)).ToList();
                }

                private IEnumerable<HttpMultipartSubStream> GetBoundarySubStreams()
                {
                    var boundarySubStreams = new List<HttpMultipartSubStream>();
                    var boundaryStart = GetNextBoundaryPosition();

                    while (boundaryStart > -1)
                    {
                        var boundaryEnd = GetNextBoundaryPosition();

                        boundarySubStreams.Add(new HttpMultipartSubStream(
                            _requestStream,
                            boundaryStart,
                            GetActualEndOfBoundary(boundaryEnd)));

                        boundaryStart = boundaryEnd;
                    }

                    return boundarySubStreams;
                }

                private long GetActualEndOfBoundary(long boundaryEnd)
                {
                    if (CheckIfFoundEndOfStream())
                    {
                        return _requestStream.Position - (_readBuffer.Length + 2);
                    }

                    return boundaryEnd - (_readBuffer.Length + 2);
                }

                private bool CheckIfFoundEndOfStream()
                {
                    return _requestStream.Position.Equals(_requestStream.Length);
                }

                private static byte[] GetBoundaryAsBytes(string boundary)
                {
                    var boundaryBuilder = new StringBuilder();

                    boundaryBuilder.Append("--");
                    boundaryBuilder.Append(boundary);
                    boundaryBuilder.Append('\r');
                    boundaryBuilder.Append('\n');

                    var bytes =
                        Encoding.ASCII.GetBytes(boundaryBuilder.ToString());

                    return bytes;
                }

                private long GetNextBoundaryPosition()
                {
                    _readBuffer.Reset();
                    while (true)
                    {
                        var byteReadFromStream = _requestStream.ReadByte();

                        if (byteReadFromStream == -1)
                        {
                            return -1;
                        }

                        _readBuffer.Insert((byte)byteReadFromStream);

                        if (_readBuffer.IsFull && _readBuffer.IsBoundary)
                        {
                            return _requestStream.Position;
                        }

                        if (byteReadFromStream.Equals(Lf) || _readBuffer.IsFull)
                        {
                            _readBuffer.Reset();
                        }
                    }
                }

                public class HttpMultipartBuffer
                {
                    private readonly byte[] _boundaryAsBytes;
                    private readonly byte[] _buffer;
                    private int _position;

                    public HttpMultipartBuffer(byte[] boundaryAsBytes)
                    {
                        _boundaryAsBytes = boundaryAsBytes;
                        _buffer = new byte[_boundaryAsBytes.Length];
                    }

                    public bool IsBoundary => _buffer.SequenceEqual(_boundaryAsBytes);

                    public bool IsFull => _position.Equals(_buffer.Length);

                    public int Length => _buffer.Length;

                    public void Reset()
                    {
                        _position = 0;
                    }

                    public void Insert(byte value)
                    {
                        _buffer[_position++] = value;
                    }
                }

                public class HttpMultipartBoundary
                {
                    private const byte MultiLf = (byte)'\n';
                    private const byte MultiCr = (byte)'\r';

                    public HttpMultipartBoundary(HttpMultipartSubStream boundaryStream)
                    {
                        Value = boundaryStream;
                        ExtractHeaders();
                    }

                    public string ContentType { get; private set; }
                    public string Filename { get; private set; }
                    public string Name { get; private set; }

                    public HttpMultipartSubStream Value { get; }

                    private void ExtractHeaders()
                    {
                        while (true)
                        {
                            var header =
                                ReadLineFromStream();

                            if (string.IsNullOrEmpty(header))
                            {
                                break;
                            }

                            if (header.StartsWith("Content-Disposition", StringComparison.CurrentCultureIgnoreCase))
                            {
                                Name = Regex.Match(header, @"name=""(?<name>[^\""]*)", RegexOptions.IgnoreCase).Groups["name"].Value;
                                Filename = Regex.Match(header, @"filename=""(?<filename>[^\""]*)", RegexOptions.IgnoreCase).Groups["filename"].Value;
                            }

                            if (header.StartsWith("Content-Type", StringComparison.InvariantCultureIgnoreCase))
                            {
                                ContentType = header.Split(' ').Last().Trim();
                            }
                        }

                        Value.PositionStartAtCurrentLocation();
                    }

                    private string ReadLineFromStream()
                    {
                        var readBuffer = new StringBuilder();

                        while (true)
                        {
                            var byteReadFromStream = Value.ReadByte();

                            if (byteReadFromStream == -1)
                            {
                                return null;
                            }

                            if (byteReadFromStream.Equals(MultiLf))
                            {
                                break;
                            }

                            readBuffer.Append((char)byteReadFromStream);
                        }

                        var lineReadFromStream =
                            readBuffer.ToString().Trim((char)MultiCr);

                        return lineReadFromStream;
                    }
                }

                public class HttpMultipartSubStream : Stream
                {
                    private readonly Stream _stream;
                    private long _start;
                    private readonly long _end;
                    private long _position;

                    public HttpMultipartSubStream(Stream stream, long start, long end)
                    {
                        _stream = stream;
                        _start = start;
                        _position = start;
                        _end = end;
                    }

                    public override bool CanRead => true;

                    public override bool CanSeek => true;

                    public override bool CanWrite => false;

                    public override long Length => (_end - _start);

                    public override long Position
                    {
                        get { return _position - _start; }
                        set { _position = Seek(value, SeekOrigin.Begin); }
                    }

                    private long CalculateSubStreamRelativePosition(SeekOrigin origin, long offset)
                    {
                        var subStreamRelativePosition = 0L;

                        switch (origin)
                        {
                            case SeekOrigin.Begin:
                                subStreamRelativePosition = _start + offset;
                                break;

                            case SeekOrigin.Current:
                                subStreamRelativePosition = _position + offset;
                                break;

                            case SeekOrigin.End:
                                subStreamRelativePosition = _end + offset;
                                break;
                        }
                        return subStreamRelativePosition;
                    }

                    public void PositionStartAtCurrentLocation()
                    {
                        _start = _stream.Position;
                    }

                    public override void Flush()
                    {
                    }

                    public override int Read(byte[] buffer, int offset, int count)
                    {
                        if (count > (_end - _position))
                        {
                            count = (int)(_end - _position);
                        }

                        if (count <= 0)
                        {
                            return 0;
                        }

                        _stream.Position = _position;

                        var bytesReadFromStream =
                            _stream.Read(buffer, offset, count);

                        RepositionAfterRead(bytesReadFromStream);

                        return bytesReadFromStream;
                    }

                    public override int ReadByte()
                    {
                        if (_position >= _end)
                        {
                            return -1;
                        }

                        _stream.Position = _position;

                        var byteReadFromStream = _stream.ReadByte();

                        RepositionAfterRead(1);

                        return byteReadFromStream;
                    }

                    private void RepositionAfterRead(int bytesReadFromStream)
                    {
                        if (bytesReadFromStream == -1)
                        {
                            _position = _end;
                        }
                        else
                        {
                            _position += bytesReadFromStream;
                        }
                    }

                    public override long Seek(long offset, SeekOrigin origin)
                    {
                        var subStreamRelativePosition =
                            CalculateSubStreamRelativePosition(origin, offset);

                        ThrowExceptionIsPositionIsOutOfBounds(subStreamRelativePosition);

                        _position = _stream.Seek(subStreamRelativePosition, SeekOrigin.Begin);

                        return _position;
                    }

                    public override void SetLength(long value)
                    {
                        throw new InvalidOperationException();
                    }

                    public override void Write(byte[] buffer, int offset, int count)
                    {
                        throw new InvalidOperationException();
                    }

                    private void ThrowExceptionIsPositionIsOutOfBounds(long subStreamRelativePosition)
                    {
                        if (subStreamRelativePosition < 0 || subStreamRelativePosition > _end)
                            throw new InvalidOperationException();
                    }
                }
            }

            public class FormData
            {
                public FormData(ReadableStringCollection form, IEnumerable<HttpFile> files)
                {
                    Form = form;
                    Files = files;
                }

                public ReadableStringCollection Form { get; }
                public IEnumerable<HttpFile> Files { get; }
            }

            public class ReadableStringCollection : IEnumerable<KeyValuePair<string, string[]>>
            {
                public ReadableStringCollection(IDictionary<string, string[]> store)
                {
                    if (store == null)
                        throw new ArgumentNullException(nameof(store));

                    Store = store;
                }

                private IDictionary<string, string[]> Store { get; }

                public string this[string key] => Get(key);

                public string Get(string key)
                {
                    return GetJoinedValue(key);
                }

                public IList<string> GetValues(string key)
                {
                    string[] values;
                    Store.TryGetValue(key, out values);
                    return values;
                }

                public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
                {
                    return Store.GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }

                private string GetJoinedValue(string key)
                {
                    var values = GetUnmodifiedValues(key);
                    return values == null ? null : string.Join(",", values);
                }

                private string[] GetUnmodifiedValues(string key)
                {
                    string[] values;
                    return Store.TryGetValue(key, out values) ? values : null;
                }
            }
        }

        internal class WebResponse
        {
            private readonly IDictionary<string, object> _environment;

            public WebResponse(IDictionary<string, object> environment)
            {
                _environment = environment;
            }

            public int StatusCode
            {
                get { return _environment.Get(OwinConstants.ResponseStatusCode, 200); }
                set { Set(OwinConstants.ResponseStatusCode, value); }
            }

            public string ReasonPhrase
            {
                get { return _environment.Get<string>(OwinConstants.ResponseReasonPhrase); }
                set { Set(OwinConstants.ResponseReasonPhrase, value); }
            }

            public string Protocol
            {
                get { return _environment.Get<string>(OwinConstants.ResponseProtocol); }
                set { Set(OwinConstants.ResponseProtocol, value); }
            }

            public ResponseHeaders Headers => new ResponseHeaders(_environment.Get<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders, new Dictionary<string, string[]>()));
            public ResponseCookieCollection Cookies => new ResponseCookieCollection(Headers);

            public Stream Body
            {
                get { return _environment.Get<Stream>(OwinConstants.ResponseBody); }
                set { Set(OwinConstants.ResponseBody, value); }
            }

            public virtual Task Write(string text)
            {
                return Write(text, CancellationToken.None);
            }

            public virtual Task Write(string text, CancellationToken token)
            {
                return Write(Encoding.UTF8.GetBytes(text), token);
            }

            public virtual Task Write(byte[] data)
            {
                return Write(data, CancellationToken.None);
            }

            public virtual Task Write(byte[] data, CancellationToken token)
            {
                return Write(data, 0, data?.Length ?? 0, token);
            }

            public virtual Task Write(byte[] data, int offset, int count, CancellationToken token)
            {
                Headers.ContentLength += count;
                return Body.WriteAsync(data, offset, count, token);
            }

            private void Set(string key, object value)
            {
                _environment[key] = value;
            }

            public class ResponseHeaders
            {
                internal const string HttpDateFormat = "r";

                public ResponseHeaders(IDictionary<string, string[]> rawHeaders)
                {
                    RawHeaders = rawHeaders;
                }

                public IDictionary<string, string[]> RawHeaders { get; }

                public virtual long? ContentLength
                {
                    get
                    {
                        long value;
                        if (long.TryParse(GetHeader(HeadersConstants.ContentLength), out value))
                            return value;

                        return null;
                    }
                    set
                    {
                        if (value.HasValue)
                            SetHeader(HeadersConstants.ContentLength, value.Value.ToString(CultureInfo.InvariantCulture));
                        else
                            RawHeaders.Remove(HeadersConstants.ContentLength);
                    }
                }

                public string ContentType
                {
                    get { return GetHeader(HeadersConstants.ContentType); }
                    set { SetHeader(HeadersConstants.ContentType, value); }
                }

                public string Location
                {
                    get { return GetHeader(HeadersConstants.Location); }
                    set { SetHeader(HeadersConstants.Location, value); }
                }

                public string CacheControl
                {
                    get { return GetHeader(HeadersConstants.CacheControl); }
                    set { SetHeader(HeadersConstants.CacheControl, value); }
                }

                public string Pragma
                {
                    get { return GetHeader(HeadersConstants.Pragma); }
                    set { SetHeader(HeadersConstants.Pragma, value); }
                }

                public string Vary
                {
                    get { return GetHeader(HeadersConstants.Vary); }
                    set { SetHeader(HeadersConstants.Vary, value); }
                }

                public DateTimeOffset? Expires
                {
                    get
                    {
                        DateTimeOffset value;
                        if (DateTimeOffset.TryParse(GetHeader(HeadersConstants.Expires),
                            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value))
                        {
                            return value;
                        }
                        return null;
                    }
                    set
                    {
                        if (value.HasValue)
                            SetHeader(HeadersConstants.Expires, value.Value.ToString(HttpDateFormat, CultureInfo.InvariantCulture));
                        else
                            RawHeaders.Remove(HeadersConstants.Expires);
                    }
                }
                public string ETag
                {
                    get { return GetHeader(HeadersConstants.ETag); }
                    set { SetHeader(HeadersConstants.ETag, value); }
                }

                public string GetHeader(string key)
                {
                    var values = GetHeaderUnmodified(key);
                    return values == null ? null : string.Join(",", values);
                }

                public void SetHeader(string key, string value)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        throw new ArgumentNullException(nameof(key));

                    if (string.IsNullOrWhiteSpace(value))
                        RawHeaders.Remove(key);
                    else
                        RawHeaders[key] = new[] { value };
                }

                public IList<string> GetValues(string key)
                {
                    return GetHeaderUnmodified(key);
                }

                public void SetValues(string key, params string[] values)
                {
                    SetHeaderUnmodified(key, values);
                }

                public void AppendValues(string key, params string[] values)
                {
                    if (values == null || values.Length == 0)
                        return;

                    var existing = GetHeaderUnmodified(key);
                    SetHeaderUnmodified(key, existing?.Concat(values) ?? values);
                }

                private string[] GetHeaderUnmodified(string key)
                {
                    string[] values;
                    return RawHeaders.TryGetValue(key, out values) ? values : null;
                }

                private void SetHeaderUnmodified(string key, IEnumerable<string> values)
                {
                    RawHeaders[key] = values.ToArray();
                }
            }

            public class ResponseCookieCollection
            {
                private readonly ResponseHeaders _headers;

                public ResponseCookieCollection(ResponseHeaders headers)
                {
                    if (headers == null)
                        throw new ArgumentNullException(nameof(headers));

                    _headers = headers;
                }

                public void Append(string key, string value)
                {
                    _headers.AppendValues(HeadersConstants.SetCookie, Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "; path=/");
                }

                public void Append(string key, string value, CookieOptions options)
                {
                    if (options == null)
                        throw new ArgumentNullException(nameof(options));

                    var domainHasValue = !string.IsNullOrEmpty(options.Domain);
                    var pathHasValue = !string.IsNullOrEmpty(options.Path);
                    var expiresHasValue = options.Expires.HasValue;

                    var setCookieValue = string.Concat(
                        Uri.EscapeDataString(key),
                        "=",
                        Uri.EscapeDataString(value ?? string.Empty),
                        !domainHasValue ? null : "; domain=",
                        !domainHasValue ? null : options.Domain,
                        !pathHasValue ? null : "; path=",
                        !pathHasValue ? null : options.Path,
                        !expiresHasValue ? null : "; expires=",
                        !expiresHasValue ? null : options.Expires.Value.ToString("ddd, dd-MMM-yyyy HH:mm:ss ", CultureInfo.InvariantCulture) + "GMT",
                        !options.Secure ? null : "; secure",
                        !options.HttpOnly ? null : "; HttpOnly");

                    _headers.AppendValues("Set-Cookie", setCookieValue);
                }

                public void Delete(string key)
                {
                    Func<string, bool> predicate = value => value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase);

                    var deleteCookies = new[] { Uri.EscapeDataString(key) + "=; expires=Thu, 01-Jan-1970 00:00:00 GMT" };

                    IList<string> existingValues = _headers.GetValues(HeadersConstants.SetCookie);
                    if (existingValues == null || existingValues.Count == 0)
                    {
                        _headers.SetValues(HeadersConstants.SetCookie, deleteCookies);
                    }
                    else
                    {
                        _headers.SetValues(HeadersConstants.SetCookie, existingValues.Where(value => !predicate(value)).Concat(deleteCookies).ToArray());
                    }
                }

                public void Delete(string key, CookieOptions options)
                {
                    if (options == null)
                        throw new ArgumentNullException(nameof(options));

                    var domainHasValue = !string.IsNullOrEmpty(options.Domain);
                    var pathHasValue = !string.IsNullOrEmpty(options.Path);

                    Func<string, bool> rejectPredicate;
                    if (domainHasValue)
                        rejectPredicate = value => value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) && value.IndexOf("domain=" + options.Domain, StringComparison.OrdinalIgnoreCase) != -1;
                    else if (pathHasValue)
                        rejectPredicate = value => value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) && value.IndexOf("path=" + options.Path, StringComparison.OrdinalIgnoreCase) != -1;
                    else
                        rejectPredicate = value => value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase);

                    var existingValues = _headers.GetValues(HeadersConstants.SetCookie);
                    if (existingValues != null)
                        _headers.SetValues(HeadersConstants.SetCookie, existingValues.Where(value => !rejectPredicate(value)).ToArray());

                    Append(key, string.Empty, new CookieOptions
                    {
                        Path = options.Path,
                        Domain = options.Domain,
                        Expires = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    });
                }
            }
        }

        public class CookieOptions
        {
            public CookieOptions()
            {
                Path = "/";
            }

            public string Domain { get; set; }
            public string Path { get; set; }
            public DateTime? Expires { get; set; }
            public bool Secure { get; set; }
            public bool HttpOnly { get; set; }
        }
    }
}
