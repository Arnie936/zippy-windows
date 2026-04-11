using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ClickyWindows
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception exception)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-error.log");
                File.WriteAllText(logPath, exception.ToString(), Encoding.UTF8);
                MessageBox.Show(exception.ToString(), "Zippy startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal sealed class AppSettings
    {
        public string ClaudeModel { get; set; }
        public bool SpeakResponses { get; set; }
        public int MaxConversationTurns { get; set; }

        public static string StorageRoot
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"); }
        }

        public static string SettingsPath
        {
            get { return Path.Combine(StorageRoot, "settings.json"); }
        }

        public static AppSettings Load()
        {
            Directory.CreateDirectory(StorageRoot);

            if (!File.Exists(SettingsPath))
            {
                AppSettings defaults = CreateDefaults();
                defaults.Save();
                return defaults;
            }

            try
            {
                JavaScriptSerializer serializer = CreateSerializer();
                AppSettings loaded = serializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath, Encoding.UTF8));
                if (loaded == null)
                {
                    loaded = CreateDefaults();
                }

                if (string.IsNullOrWhiteSpace(loaded.ClaudeModel))
                {
                    loaded.ClaudeModel = "claude-sonnet-4-6";
                }

                if (loaded.MaxConversationTurns <= 0)
                {
                    loaded.MaxConversationTurns = 10;
                }

                return loaded;
            }
            catch
            {
                AppSettings defaults = CreateDefaults();
                defaults.Save();
                return defaults;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(StorageRoot);
            JavaScriptSerializer serializer = CreateSerializer();
            string json = serializer.Serialize(this);
            File.WriteAllText(SettingsPath, json, Encoding.UTF8);
        }

        private static AppSettings CreateDefaults()
        {
            return new AppSettings
            {
                ClaudeModel = "claude-sonnet-4-6",
                SpeakResponses = true,
                MaxConversationTurns = 10
            };
        }

        private static JavaScriptSerializer CreateSerializer()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            return serializer;
        }
    }

    internal sealed class EnvironmentConfiguration
    {
        public string AnthropicApiKey { get; set; }
        public string ElevenLabsApiKey { get; set; }
        public string ElevenLabsVoiceId { get; set; }
        public string SpeechToTextProvider { get; set; }
        public string CodexCommand { get; set; }
        public string CodexWorkingDirectory { get; set; }
        public int CodexTimeoutSeconds { get; set; }
        public string WhisperPythonCommand { get; set; }
        public string WhisperModel { get; set; }
        public string WhisperLanguage { get; set; }
        public string PushToTalkKey { get; set; }

        public static string EnvFilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"); }
        }

        public static EnvironmentConfiguration Load()
        {
            Dictionary<string, string> envValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(EnvFilePath))
            {
                foreach (string rawLine in File.ReadAllLines(EnvFilePath, Encoding.UTF8))
                {
                    string trimmedLine = rawLine.Trim();
                    if (trimmedLine.Length == 0 || trimmedLine.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    string key = trimmedLine.Substring(0, separatorIndex).Trim();
                    string value = trimmedLine.Substring(separatorIndex + 1).Trim();

                    if (value.Length >= 2 && value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    envValues[key] = value;
                }
            }

            return new EnvironmentConfiguration
            {
                AnthropicApiKey = GetValueOrEmpty(envValues, "ANTHROPIC_API_KEY"),
                ElevenLabsApiKey = GetValueOrEmpty(envValues, "ELEVENLABS_API_KEY"),
                ElevenLabsVoiceId = GetValueOrEmpty(envValues, "ELEVENLABS_VOICE_ID"),
                SpeechToTextProvider = GetSpeechToTextProvider(envValues),
                CodexCommand = GetValueOrDefault(envValues, "CODEX_COMMAND", "codex.cmd"),
                CodexWorkingDirectory = GetValueOrDefault(envValues, "CODEX_WORKDIR", GetDefaultCodexWorkingDirectory()),
                CodexTimeoutSeconds = GetIntValueOrDefault(envValues, "CODEX_TIMEOUT_SECONDS", 900),
                WhisperPythonCommand = GetValueOrDefault(envValues, "WHISPER_PYTHON", "python"),
                WhisperModel = GetValueOrDefault(envValues, "WHISPER_MODEL", "base"),
                WhisperLanguage = GetValueOrDefault(envValues, "WHISPER_LANGUAGE", "de"),
                PushToTalkKey = GetValueOrDefault(envValues, "PUSH_TO_TALK_KEY", "F8")
            };
        }

        public string Validate()
        {
            List<string> missingKeys = new List<string>();

            if (string.IsNullOrWhiteSpace(AnthropicApiKey))
            {
                missingKeys.Add("ANTHROPIC_API_KEY");
            }

            if (string.IsNullOrWhiteSpace(ElevenLabsApiKey))
            {
                missingKeys.Add("ELEVENLABS_API_KEY");
            }

            if (string.IsNullOrWhiteSpace(ElevenLabsVoiceId))
            {
                missingKeys.Add("ELEVENLABS_VOICE_ID");
            }

            if (missingKeys.Count == 0)
            {
                return null;
            }

            return ".env is missing: " + string.Join(", ", missingKeys);
        }

        private static string GetValueOrEmpty(IDictionary<string, string> values, string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : string.Empty;
        }

        private static string GetValueOrDefault(IDictionary<string, string> values, string key, string defaultValue)
        {
            string value;
            return values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
        }

        private static string GetSpeechToTextProvider(IDictionary<string, string> values)
        {
            string provider = GetValueOrDefault(values, "STT_PROVIDER", "whisper").Trim().ToLowerInvariant();
            return provider == "elevenlabs" ? "elevenlabs" : "whisper";
        }

        private static int GetIntValueOrDefault(IDictionary<string, string> values, string key, int defaultValue)
        {
            string rawValue;
            int parsedValue;
            return values.TryGetValue(key, out rawValue) && int.TryParse(rawValue, out parsedValue) && parsedValue > 0
                ? parsedValue
                : defaultValue;
        }

        private static string GetDefaultCodexWorkingDirectory()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(Path.GetFullPath(Path.Combine(baseDirectory, "..")), "playground");
        }
    }

    internal sealed class ConversationTurn
    {
        public string UserTranscript { get; set; }
        public string AssistantResponse { get; set; }
    }

    internal sealed class PointTagResult
    {
        private static readonly Regex PointRegex = new Regex(@"\[POINT:(?:none|(\d+)\s*,\s*(\d+)(?::([^\]:\s][^\]:]*?))?(?::screen(\d+))?)\]\s*$", RegexOptions.Compiled);

        public string SpokenText { get; set; }
        public Point? Coordinate { get; set; }
        public string ElementLabel { get; set; }
        public int? ScreenNumber { get; set; }

        public static PointTagResult Parse(string responseText)
        {
            Match match = PointRegex.Match(responseText ?? string.Empty);
            if (!match.Success)
            {
                return new PointTagResult
                {
                    SpokenText = (responseText ?? string.Empty).Trim()
                };
            }

            string spokenText = (responseText ?? string.Empty).Substring(0, match.Index).Trim();
            if (!match.Groups[1].Success || !match.Groups[2].Success)
            {
                return new PointTagResult
                {
                    SpokenText = spokenText,
                    ElementLabel = "none"
                };
            }

            PointTagResult parsedResult = new PointTagResult
            {
                SpokenText = spokenText,
                Coordinate = new Point(
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value)
                )
            };

            if (match.Groups[3].Success)
            {
                parsedResult.ElementLabel = match.Groups[3].Value.Trim();
            }

            if (match.Groups[4].Success)
            {
                parsedResult.ScreenNumber = int.Parse(match.Groups[4].Value);
            }

            return parsedResult;
        }
    }

    internal sealed class ScreenCaptureInfo
    {
        public int ScreenNumber { get; set; }
        public string Label { get; set; }
        public bool IsCursorScreen { get; set; }
        public int ScreenshotWidth { get; set; }
        public int ScreenshotHeight { get; set; }
        public Rectangle DisplayBounds { get; set; }
        public string ImageBase64 { get; set; }
        public byte[] ImageBytes { get; set; }
    }

    internal enum CompanionVisualState
    {
        Idle,
        Listening,
        Transcribing,
        Thinking,
        Speaking
    }

    internal static class ScreenCaptureService
    {
        public static List<ScreenCaptureInfo> CaptureAllScreens()
        {
            Screen[] screens = Screen.AllScreens;
            if (screens.Length == 0)
            {
                throw new InvalidOperationException("Windows did not report any screens to capture.");
            }

            Point cursorPosition = Cursor.Position;
            List<Screen> orderedScreens = screens
                .OrderBy(screen => screen.Bounds.Contains(cursorPosition) ? 0 : 1)
                .ToList();

            List<ScreenCaptureInfo> captures = new List<ScreenCaptureInfo>();

            for (int index = 0; index < orderedScreens.Count; index++)
            {
                Screen screen = orderedScreens[index];
                Rectangle bounds = screen.Bounds;
                bool isCursorScreen = bounds.Contains(cursorPosition);

                using (Bitmap originalBitmap = new Bitmap(bounds.Width, bounds.Height))
                using (Graphics graphics = Graphics.FromImage(originalBitmap))
                {
                    graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

                    const double maxDimension = 1280.0;
                    double scaleRatio = Math.Min(maxDimension / bounds.Width, maxDimension / bounds.Height);
                    scaleRatio = Math.Min(scaleRatio, 1.0);

                    int scaledWidth = Math.Max(1, (int)Math.Round(bounds.Width * scaleRatio));
                    int scaledHeight = Math.Max(1, (int)Math.Round(bounds.Height * scaleRatio));

                    using (Bitmap scaledBitmap = new Bitmap(scaledWidth, scaledHeight))
                    using (Graphics scaledGraphics = Graphics.FromImage(scaledBitmap))
                    {
                        scaledGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        scaledGraphics.SmoothingMode = SmoothingMode.HighQuality;
                        scaledGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        scaledGraphics.DrawImage(originalBitmap, 0, 0, scaledWidth, scaledHeight);

                        string screenLabel;
                        if (orderedScreens.Count == 1)
                        {
                            screenLabel = "user's screen (cursor is here)";
                        }
                        else if (isCursorScreen)
                        {
                            screenLabel = string.Format("screen {0} of {1} - cursor is on this screen (primary focus)", index + 1, orderedScreens.Count);
                        }
                        else
                        {
                            screenLabel = string.Format("screen {0} of {1} - secondary screen", index + 1, orderedScreens.Count);
                        }

                        captures.Add(new ScreenCaptureInfo
                        {
                            ScreenNumber = index + 1,
                            Label = string.Format("{0} (image dimensions: {1}x{2} pixels)", screenLabel, scaledWidth, scaledHeight),
                            IsCursorScreen = isCursorScreen,
                            ScreenshotWidth = scaledWidth,
                            ScreenshotHeight = scaledHeight,
                            DisplayBounds = bounds,
                            ImageBytes = EncodeJpeg(scaledBitmap, 82L)
                        });
                        captures[captures.Count - 1].ImageBase64 = Convert.ToBase64String(captures[captures.Count - 1].ImageBytes);
                    }
                }
            }

            return captures;
        }

        private static byte[] EncodeJpeg(Bitmap bitmap, long quality)
        {
            ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders().First(codec => codec.MimeType == "image/jpeg");
            EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, jpegCodec, encoderParameters);
                return memoryStream.ToArray();
            }
        }
    }

    internal static class DirectApiClient
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static readonly JavaScriptSerializer Serializer = CreateSerializer();
        private const string AnthropicMessagesUrl = "https://api.anthropic.com/v1/messages";
        private const string ElevenLabsTextToSpeechUrlFormat = "https://api.elevenlabs.io/v1/text-to-speech/{0}";
        private const string ElevenLabsSpeechToTextUrl = "https://api.elevenlabs.io/v1/speech-to-text";
        private const string ElevenLabsSpeechToTextModel = "scribe_v2";
        private const string CompanionPrompt = @"you're zippy, a friendly always-on companion living on the user's windows desktop. the user just asked you a question and you can see their screen or screens. your reply may be shown on screen and optionally spoken aloud, so write the way you'd naturally talk.

rules:
- default to one or two sentences unless the user clearly wants depth.
- all lowercase, casual, warm, direct. no emojis.
- write for the ear. avoid lists, markdown, and stiff formatting.
- if the user's question relates to something visible on screen, reference the specific thing you can actually see.
- if the screenshots are not relevant, answer directly.
- never say ""simply"" or ""just"".
- do not read code verbatim unless the user explicitly asks for it.
- if you receive multiple screen images, the one labeled ""primary focus"" is where the cursor is. prioritize that one.

element pointing:
you can point at a specific place on screen. use that whenever it would genuinely help the user find a control, button, tab, panel, or other visual target.

if the user asks you to go to, move to, or navigate to something visible on screen, return a point for that destination.
for requests like ""go to the telegram window"" or ""geh zum telegram fenster"", point at the visible target window if you can see it.

when you point, append a coordinate tag at the very end of the response using the screenshot pixel coordinates:
[POINT:x,y:label]

if the element is on another screen, append :screenN:
[POINT:400,300:terminal:screen2]

if pointing would not help, append [POINT:none].";

        public static async Task<string> SmokeTestAsync(AppSettings settings, EnvironmentConfiguration environmentConfiguration)
        {
            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                { "model", settings.ClaudeModel },
                { "max_tokens", 24 },
                { "stream", false },
                { "system", "reply with only the word ready" },
                { "messages", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "role", "user" },
                            { "content", "say ready" }
                        }
                    }
                }
            };

            string responseBody = await PostAnthropicAsync(requestBody, environmentConfiguration.AnthropicApiKey).ConfigureAwait(false);
            return ExtractText(responseBody);
        }

        public static async Task<string> AskAsync(
            AppSettings settings,
            EnvironmentConfiguration environmentConfiguration,
            string prompt,
            IList<ScreenCaptureInfo> screenCaptures,
            IList<ConversationTurn> conversationHistory)
        {
            List<object> messages = new List<object>();
            foreach (ConversationTurn turn in conversationHistory)
            {
                messages.Add(new Dictionary<string, object>
                {
                    { "role", "user" },
                    { "content", turn.UserTranscript }
                });
                messages.Add(new Dictionary<string, object>
                {
                    { "role", "assistant" },
                    { "content", turn.AssistantResponse }
                });
            }

            List<object> contentBlocks = new List<object>();
            foreach (ScreenCaptureInfo screenCapture in screenCaptures)
            {
                contentBlocks.Add(new Dictionary<string, object>
                {
                    { "type", "image" },
                    { "source", new Dictionary<string, object>
                        {
                            { "type", "base64" },
                            { "media_type", "image/jpeg" },
                            { "data", screenCapture.ImageBase64 }
                        }
                    }
                });
                contentBlocks.Add(new Dictionary<string, object>
                {
                    { "type", "text" },
                    { "text", screenCapture.Label }
                });
            }

            contentBlocks.Add(new Dictionary<string, object>
            {
                { "type", "text" },
                { "text", prompt }
            });

            messages.Add(new Dictionary<string, object>
            {
                { "role", "user" },
                { "content", contentBlocks.ToArray() }
            });

            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                { "model", settings.ClaudeModel },
                { "max_tokens", 1024 },
                { "stream", false },
                { "system", CompanionPrompt },
                { "messages", messages.ToArray() }
            };

            string responseBody = await PostAnthropicAsync(requestBody, environmentConfiguration.AnthropicApiKey).ConfigureAwait(false);
            return ExtractText(responseBody);
        }

        public static async Task<byte[]> SynthesizeSpeechAsync(EnvironmentConfiguration environmentConfiguration, string text)
        {
            string ttsUrl = string.Format(ElevenLabsTextToSpeechUrlFormat, environmentConfiguration.ElevenLabsVoiceId);

            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                { "text", text },
                { "model_id", "eleven_flash_v2_5" },
                { "voice_settings", new Dictionary<string, object>
                    {
                        { "stability", 0.5 },
                        { "similarity_boost", 0.75 }
                    }
                }
            };

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ttsUrl);
            request.Content = new StringContent(Serializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            request.Headers.Accept.ParseAdd("audio/mpeg");
            request.Headers.Add("xi-api-key", environmentConfiguration.ElevenLabsApiKey);

            HttpResponseMessage response = await HttpClient.SendAsync(request).ConfigureAwait(false);
            byte[] responseBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(string.Format(
                    "TTS proxy error ({0}): {1}",
                    (int)response.StatusCode,
                    Encoding.UTF8.GetString(responseBytes)
                ));
            }

            return responseBytes;
        }

        public static async Task<string> TranscribeSpeechWithElevenLabsAsync(EnvironmentConfiguration environmentConfiguration, string audioFilePath)
        {
            using (MultipartFormDataContent content = new MultipartFormDataContent())
            {
                ByteArrayContent audioContent = new ByteArrayContent(File.ReadAllBytes(audioFilePath));
                audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                content.Add(audioContent, "file", Path.GetFileName(audioFilePath));
                content.Add(new StringContent(ElevenLabsSpeechToTextModel), "model_id");

                if (!string.IsNullOrWhiteSpace(environmentConfiguration.WhisperLanguage))
                {
                    content.Add(new StringContent(environmentConfiguration.WhisperLanguage), "language_code");
                }

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ElevenLabsSpeechToTextUrl);
                request.Headers.Add("xi-api-key", environmentConfiguration.ElevenLabsApiKey);
                request.Content = content;

                HttpResponseMessage response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(string.Format(
                        "ElevenLabs speech-to-text error ({0}): {1}",
                        (int)response.StatusCode,
                        responseText
                    ));
                }

                string transcript = ExtractSpeechToTextTranscript(responseText);
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    throw new InvalidOperationException("ElevenLabs returned an empty transcript.");
                }

                return transcript.Trim();
            }
        }

        private static async Task<string> PostAnthropicAsync(object body, string anthropicApiKey)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, AnthropicMessagesUrl);
            request.Headers.Add("x-api-key", anthropicApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            StringContent content = new StringContent(Serializer.Serialize(body), Encoding.UTF8, "application/json");
            request.Content = content;

            HttpResponseMessage response = await HttpClient.SendAsync(request).ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(string.Format(
                    "Anthropic error ({0}): {1}",
                    (int)response.StatusCode,
                    responseText
                ));
            }

            return responseText;
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(4);
            return httpClient;
        }

        private static string ExtractText(string responseBody)
        {
            object rootObject = Serializer.DeserializeObject(responseBody);
            Dictionary<string, object> rootDictionary = rootObject as Dictionary<string, object>;
            if (rootDictionary == null || !rootDictionary.ContainsKey("content"))
            {
                throw new InvalidOperationException("Claude returned an unexpected response body.");
            }

            object[] contentArray = rootDictionary["content"] as object[];
            if (contentArray == null)
            {
                throw new InvalidOperationException("Claude response did not contain text blocks.");
            }

            StringBuilder textBuilder = new StringBuilder();
            foreach (object contentItem in contentArray)
            {
                Dictionary<string, object> contentDictionary = contentItem as Dictionary<string, object>;
                if (contentDictionary == null)
                {
                    continue;
                }

                object typeValue;
                if (!contentDictionary.TryGetValue("type", out typeValue) || !string.Equals(typeValue as string, "text", StringComparison.Ordinal))
                {
                    continue;
                }

                object textValue;
                if (contentDictionary.TryGetValue("text", out textValue) && textValue != null)
                {
                    if (textBuilder.Length > 0)
                    {
                        textBuilder.AppendLine();
                    }
                    textBuilder.Append(textValue.ToString());
                }
            }

            string extractedText = textBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new InvalidOperationException("Claude returned an empty text response.");
            }

            return extractedText;
        }

        private static JavaScriptSerializer CreateSerializer()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            return serializer;
        }

        private static string ExtractSpeechToTextTranscript(string responseText)
        {
            object parsed = Serializer.DeserializeObject(responseText);
            Dictionary<string, object> root = parsed as Dictionary<string, object>;
            if (root == null)
            {
                return string.Empty;
            }

            object textValue;
            if (root.TryGetValue("text", out textValue) && textValue != null)
            {
                return textValue.ToString();
            }

            object wordsValue;
            object[] words = null;
            if (root.TryGetValue("words", out wordsValue))
            {
                words = wordsValue as object[];
            }

            if (words == null || words.Length == 0)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            foreach (object wordEntry in words)
            {
                Dictionary<string, object> wordObject = wordEntry as Dictionary<string, object>;
                if (wordObject == null)
                {
                    continue;
                }

                object wordText;
                if (wordObject.TryGetValue("text", out wordText) && wordText != null)
                {
                    parts.Add(wordText.ToString());
                }
            }

            return string.Join(" ", parts.ToArray()).Trim();
        }
    }

    internal sealed class MicrophoneRecorder : IDisposable
    {
        private const string RecordingAlias = "zippyrec";
        private bool _isRecording;
        private string _recordingPath;

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr callbackHandle);

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern bool mciGetErrorString(int errorCode, StringBuilder errorText, int errorTextSize);

        public bool IsRecording
        {
            get { return _isRecording; }
        }

        public void Start()
        {
            if (_isRecording)
            {
                return;
            }

            Directory.CreateDirectory(AppSettings.StorageRoot);
            _recordingPath = Path.Combine(AppSettings.StorageRoot, "clicky-recording-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".wav");

            CloseAliasQuietly();
            SendCommand("open new type waveaudio alias " + RecordingAlias);

            try
            {
                SendCommand("set " + RecordingAlias + " time format ms");
                SendCommand("record " + RecordingAlias);
                _isRecording = true;
            }
            catch
            {
                CloseAliasQuietly();
                _recordingPath = null;
                throw;
            }
        }

        public string Stop()
        {
            if (!_isRecording)
            {
                throw new InvalidOperationException("No microphone recording is currently running.");
            }

            try
            {
                SendCommand("stop " + RecordingAlias);
                SendCommand("save " + RecordingAlias + " " + QuotePath(_recordingPath));
                return _recordingPath;
            }
            finally
            {
                CloseAliasQuietly();
                _isRecording = false;
            }
        }

        public void Cancel()
        {
            if (!_isRecording)
            {
                return;
            }

            CloseAliasQuietly();
            _isRecording = false;
        }

        public void Dispose()
        {
            Cancel();
        }

        private static void SendCommand(string command)
        {
            int errorCode = mciSendString(command, null, 0, IntPtr.Zero);
            if (errorCode == 0)
            {
                return;
            }

            StringBuilder errorText = new StringBuilder(256);
            if (!mciGetErrorString(errorCode, errorText, errorText.Capacity))
            {
                errorText.Append("unknown MCI error");
            }

            throw new InvalidOperationException("Microphone capture failed: " + errorText);
        }

        private static void CloseAliasQuietly()
        {
            mciSendString("close " + RecordingAlias, null, 0, IntPtr.Zero);
        }

        private static string QuotePath(string path)
        {
            return "\"" + path.Replace("\"", string.Empty) + "\"";
        }
    }

    internal static class WhisperClient
    {
        public static async Task<string> TranscribeAsync(EnvironmentConfiguration environmentConfiguration, string audioFilePath)
        {
            string outputDirectory = Path.Combine(AppSettings.StorageRoot, "whisper-output-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outputDirectory);

            string transcriptPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(audioFilePath) + ".txt");
            string arguments = BuildArguments(environmentConfiguration, audioFilePath, outputDirectory);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = environmentConfiguration.WhisperPythonCommand,
                Arguments = arguments,
                WorkingDirectory = AppSettings.StorageRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;

                if (!process.Start())
                {
                    throw new InvalidOperationException("Failed to start the local Whisper process.");
                }

                Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit()).ConfigureAwait(false);

                string standardOutput = await standardOutputTask.ConfigureAwait(false);
                string standardError = await standardErrorTask.ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(BuildWhisperErrorMessage(environmentConfiguration, standardError, standardOutput));
                }
            }

            if (!File.Exists(transcriptPath))
            {
                throw new InvalidOperationException("Whisper finished without writing a transcript file. Check that the local model can run and ffmpeg is installed.");
            }

            string transcript = File.ReadAllText(transcriptPath, Encoding.UTF8).Trim();
            if (string.IsNullOrWhiteSpace(transcript))
            {
                throw new InvalidOperationException("Whisper returned an empty transcript.");
            }

            TryDeleteDirectory(outputDirectory);
            return transcript;
        }

        private static string BuildArguments(EnvironmentConfiguration environmentConfiguration, string audioFilePath, string outputDirectory)
        {
            List<string> arguments = new List<string>();
            arguments.Add("-m");
            arguments.Add("whisper");
            arguments.Add(QuoteArgument(audioFilePath));
            arguments.Add("--model");
            arguments.Add(QuoteArgument(environmentConfiguration.WhisperModel));
            arguments.Add("--task");
            arguments.Add("transcribe");
            arguments.Add("--fp16");
            arguments.Add("False");
            arguments.Add("--verbose");
            arguments.Add("False");
            arguments.Add("--output_format");
            arguments.Add("txt");
            arguments.Add("--output_dir");
            arguments.Add(QuoteArgument(outputDirectory));

            if (!string.IsNullOrWhiteSpace(environmentConfiguration.WhisperLanguage))
            {
                arguments.Add("--language");
                arguments.Add(QuoteArgument(environmentConfiguration.WhisperLanguage));
            }

            return string.Join(" ", arguments.ToArray());
        }

        private static string BuildWhisperErrorMessage(EnvironmentConfiguration environmentConfiguration, string standardError, string standardOutput)
        {
            string detail = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = "Whisper exited without a detailed error message.";
            }

            return string.Format(
                "Local Whisper failed.\r\npython: {0}\r\nmodel: {1}\r\n\r\n{2}",
                environmentConfiguration.WhisperPythonCommand,
                environmentConfiguration.WhisperModel,
                detail.Trim()
            );
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static void TryDeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
            catch
            {
            }
        }
    }

    internal static class SpeechToTextClient
    {
        public static Task<string> TranscribeAsync(EnvironmentConfiguration environmentConfiguration, string audioFilePath)
        {
            if (string.Equals(environmentConfiguration.SpeechToTextProvider, "elevenlabs", StringComparison.OrdinalIgnoreCase))
            {
                return DirectApiClient.TranscribeSpeechWithElevenLabsAsync(environmentConfiguration, audioFilePath);
            }

            return WhisperClient.TranscribeAsync(environmentConfiguration, audioFilePath);
        }

        public static string GetProviderLabel(EnvironmentConfiguration environmentConfiguration)
        {
            return string.Equals(environmentConfiguration.SpeechToTextProvider, "elevenlabs", StringComparison.OrdinalIgnoreCase)
                ? "elevenlabs"
                : "local whisper";
        }
    }

    internal sealed class CodexRunResult
    {
        public int ExitCode { get; set; }
        public string OutputFilePath { get; set; }
    }

    internal static class CodexClient
    {
        private const string CompletionMessage = "codex session ist jetzt abgeschlossen";
        private static readonly Regex TriggerRegex = new Regex(@"\b(?:nimm|nim|nehm|nehm|mit)\s+(?:den\s+)?(?:codex|kodex|kodes|codecs|kodexx)\b[\s,:-]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex TriggerWithScreenRegex = new Regex(@"\b(?:nimm|nim|nehm|mit)\s+(?:den\s+)?(?:codex|kodex|kodes)\s+(?:mit|mids?|plus)\s+(?:screen|screenshot|bild|main\s*screen|hauptbildschirm|hauptscreen)\b[\s,:-]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool IsTriggered(string prompt)
        {
            string normalizedPrompt = NormalizePrompt(prompt);
            return !string.IsNullOrWhiteSpace(prompt) && (TriggerWithScreenRegex.IsMatch(normalizedPrompt) || TriggerRegex.IsMatch(normalizedPrompt));
        }

        public static string RemoveTrigger(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return string.Empty;
            }

            string normalizedPrompt = NormalizePrompt(prompt);
            normalizedPrompt = TriggerWithScreenRegex.Replace(normalizedPrompt, string.Empty, 1).Trim();
            return TriggerRegex.Replace(normalizedPrompt, string.Empty, 1).Trim();
        }

        public static bool ShouldAttachScreens(string prompt)
        {
            return !string.IsNullOrWhiteSpace(prompt) && TriggerWithScreenRegex.IsMatch(NormalizePrompt(prompt));
        }

        public static string GetCompletionMessage()
        {
            return CompletionMessage;
        }

        private static string NormalizePrompt(string prompt)
        {
            string normalized = prompt.ToLowerInvariant();
            normalized = normalized.Replace("kodex", "codex");
            normalized = normalized.Replace("kodes", "codex");
            normalized = normalized.Replace("codecs", "codex");
            normalized = normalized.Replace("codexx", "codex");
            normalized = normalized.Replace("nehm", "nimm");
            normalized = normalized.Replace("nehm", "nimm");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return normalized;
        }

        public static async Task<CodexRunResult> RunAsync(EnvironmentConfiguration environmentConfiguration, string prompt, IList<string> imagePaths = null)
        {
            string outputDirectory = GetCodexOutputDirectory();
            Directory.CreateDirectory(outputDirectory);
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string outputFilePath = Path.Combine(outputDirectory, "zippy-codex-" + timestamp + ".txt");
            string workingDirectory = ResolveWorkingDirectory(environmentConfiguration);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            AddArgument(startInfo, "/c");
            AddArgument(startInfo, ResolveCodexCommand(environmentConfiguration));
            AddArgument(startInfo, "exec");
            AddArgument(startInfo, "--full-auto");
            AddArgument(startInfo, "--skip-git-repo-check");
            AddArgument(startInfo, "-C");
            AddArgument(startInfo, workingDirectory);
            AddArgument(startInfo, "-o");
            AddArgument(startInfo, outputFilePath);
            if (imagePaths != null)
            {
                foreach (string imagePath in imagePaths)
                {
                    if (!string.IsNullOrWhiteSpace(imagePath))
                    {
                        AddArgument(startInfo, "-i");
                        AddArgument(startInfo, imagePath);
                    }
                }
            }
            AddArgument(startInfo, "-");

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;

                if (!process.Start())
                {
                    throw new InvalidOperationException("Failed to start the local Codex process.");
                }

                byte[] promptBytes = new UTF8Encoding(false).GetBytes(prompt ?? string.Empty);
                await process.StandardInput.BaseStream.WriteAsync(promptBytes, 0, promptBytes.Length).ConfigureAwait(false);
                await process.StandardInput.BaseStream.FlushAsync().ConfigureAwait(false);
                process.StandardInput.Close();

                Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
                bool exited = await Task.Run(() => process.WaitForExit(environmentConfiguration.CodexTimeoutSeconds * 1000)).ConfigureAwait(false);

                if (!exited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }

                    throw new InvalidOperationException("Codex timed out before finishing.");
                }

                string standardOutput = await standardOutputTask.ConfigureAwait(false);
                string standardError = await standardErrorTask.ConfigureAwait(false);
                WriteOutputFile(outputFilePath, workingDirectory, prompt, process.ExitCode, standardOutput, standardError);

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("Codex failed. Check the codex output file: " + outputFilePath);
                }

                return new CodexRunResult
                {
                    ExitCode = process.ExitCode,
                    OutputFilePath = outputFilePath
                };
            }
        }

        private static string ResolveWorkingDirectory(EnvironmentConfiguration environmentConfiguration)
        {
            if (!string.IsNullOrWhiteSpace(environmentConfiguration.CodexWorkingDirectory))
            {
                Directory.CreateDirectory(environmentConfiguration.CodexWorkingDirectory);
                return environmentConfiguration.CodexWorkingDirectory;
            }

            string defaultWorkingDirectory = Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..")), "playground");
            Directory.CreateDirectory(defaultWorkingDirectory);
            return defaultWorkingDirectory;
        }

        private static string GetCodexOutputDirectory()
        {
            return Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..")), "codex output");
        }

        private static void AddArgument(ProcessStartInfo startInfo, string value)
        {
            startInfo.Arguments = string.IsNullOrWhiteSpace(startInfo.Arguments)
                ? QuoteArgument(value)
                : startInfo.Arguments + " " + QuoteArgument(value);
        }

        private static string ResolveCodexCommand(EnvironmentConfiguration environmentConfiguration)
        {
            if (!string.IsNullOrWhiteSpace(environmentConfiguration.CodexCommand) && File.Exists(environmentConfiguration.CodexCommand))
            {
                return environmentConfiguration.CodexCommand;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string defaultCommandScript = Path.Combine(appData, "npm", "codex.cmd");
            if (File.Exists(defaultCommandScript))
            {
                return defaultCommandScript;
            }

            return environmentConfiguration.CodexCommand;
        }

        private static void WriteOutputFile(string outputFilePath, string workingDirectory, string prompt, int exitCode, string standardOutput, string standardError)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("zippy codex run");
            builder.AppendLine("timestamp: " + DateTime.Now.ToString("O"));
            builder.AppendLine("working_directory: " + workingDirectory);
            builder.AppendLine("exit_code: " + exitCode.ToString());
            builder.AppendLine();
            builder.AppendLine("prompt:");
            builder.AppendLine(prompt);
            builder.AppendLine();
            builder.AppendLine("stdout:");
            builder.AppendLine(string.IsNullOrWhiteSpace(standardOutput) ? "<empty>" : standardOutput.Trim());
            builder.AppendLine();
            builder.AppendLine("stderr:");
            builder.AppendLine(string.IsNullOrWhiteSpace(standardError) ? "<empty>" : standardError.Trim());
            File.WriteAllText(outputFilePath, builder.ToString(), Encoding.UTF8);
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }
    }

    internal sealed class PushToTalkHotKeyListener : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public event EventHandler HotKeyPressed;
        public event EventHandler HotKeyReleased;

        private readonly HookProc _hookProc;
        private readonly Keys _hotKey;
        private IntPtr _hookHandle;
        private bool _isPressed;

        public PushToTalkHotKeyListener(Keys hotKey)
        {
            _hotKey = hotKey;
            _hookProc = HookCallback;

            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule)
            {
                IntPtr moduleHandle = currentModule == null ? IntPtr.Zero : GetModuleHandle(currentModule.ModuleName);
                _hookHandle = SetWindowsHookEx(WhKeyboardLl, _hookProc, moduleHandle, 0);
            }
        }

        public bool IsRegistered
        {
            get { return _hookHandle != IntPtr.Zero; }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                int virtualKeyCode = Marshal.ReadInt32(lParam);

                if (virtualKeyCode == (int)_hotKey)
                {
                    if ((message == WmKeyDown || message == WmSysKeyDown) && !_isPressed)
                    {
                        _isPressed = true;
                        if (HotKeyPressed != null)
                        {
                            HotKeyPressed(this, EventArgs.Empty);
                        }
                    }
                    else if ((message == WmKeyUp || message == WmSysKeyUp) && _isPressed)
                    {
                        _isPressed = false;
                        if (HotKeyReleased != null)
                        {
                            HotKeyReleased(this, EventArgs.Empty);
                        }
                    }
                }
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }

    internal sealed class CompanionOverlayForm : Form
    {
        private const int OverlayWidth = 360;
        private const int OverlayHeight = 190;
        private readonly Timer _animationTimer;
        private PointF _displayLocation;
        private bool _locationInitialized;
        private bool _bubbleOnLeft;
        private float _phase;
        private string _bubbleText;
        private DateTime _bubbleExpiresAtUtc;
        private Point? _navigationAnchorPoint;
        private DateTime _navigationExpiresAtUtc;
        private CompanionVisualState _state;
        private CompanionVisualState _stateAfterBubble;

        public CompanionOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            DoubleBuffered = true;
            Width = OverlayWidth;
            Height = OverlayHeight;

            _state = CompanionVisualState.Idle;
            _stateAfterBubble = CompanionVisualState.Idle;

            _animationTimer = new Timer();
            _animationTimer.Interval = 33;
            _animationTimer.Tick += delegate
            {
                AdvanceAnimationFrame();
            };
            _animationTimer.Start();
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WsExTransparent = 0x20;
                const int WsExToolWindow = 0x80;
                const int WsExLayered = 0x80000;
                const int WsExNoActivate = 0x08000000;

                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= WsExTransparent | WsExToolWindow | WsExLayered | WsExNoActivate;
                return createParams;
            }
        }

        public void SetState(CompanionVisualState state)
        {
            _state = state;
            _navigationAnchorPoint = null;
            Invalidate();
        }

        public void ShowMessage(string bubbleText, CompanionVisualState state, int durationMs, CompanionVisualState stateAfterBubble)
        {
            _bubbleText = (bubbleText ?? string.Empty).Trim();
            if (_bubbleText.Length > 280)
            {
                _bubbleText = _bubbleText.Substring(0, 277) + "...";
            }

            _state = state;
            _stateAfterBubble = stateAfterBubble;
            _bubbleExpiresAtUtc = DateTime.UtcNow.AddMilliseconds(Math.Max(1500, durationMs));
            Invalidate();
        }

        public void NavigateTo(Point anchorPoint, string bubbleText, CompanionVisualState state, int durationMs, CompanionVisualState stateAfterNavigation)
        {
            _navigationAnchorPoint = anchorPoint;
            _navigationExpiresAtUtc = DateTime.UtcNow.AddMilliseconds(Math.Max(2200, durationMs));
            ShowMessage(bubbleText, state, durationMs, stateAfterNavigation);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Magenta);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float bob = (float)Math.Sin(_phase * 1.35f) * 3.0f;
            Rectangle orbRectangle = _bubbleOnLeft
                ? new Rectangle(OverlayWidth - 106, OverlayHeight - 96 + (int)bob, 70, 70)
                : new Rectangle(34, OverlayHeight - 96 + (int)bob, 70, 70);

            DrawTrail(e.Graphics, orbRectangle);

            if (!string.IsNullOrWhiteSpace(_bubbleText))
            {
                DrawBubble(e.Graphics, orbRectangle, _bubbleText);
            }

            DrawStateChip(e.Graphics, orbRectangle);
            DrawCompanionBody(e.Graphics, orbRectangle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void AdvanceAnimationFrame()
        {
            if (!string.IsNullOrWhiteSpace(_bubbleText) && DateTime.UtcNow >= _bubbleExpiresAtUtc)
            {
                _bubbleText = null;
                _state = _stateAfterBubble;
            }

            if (_navigationAnchorPoint.HasValue && DateTime.UtcNow >= _navigationExpiresAtUtc)
            {
                _navigationAnchorPoint = null;
            }

            _phase += 0.16f;

            Point focusPoint = _navigationAnchorPoint ?? Cursor.Position;
            Rectangle workingArea = Screen.FromPoint(focusPoint).WorkingArea;
            _bubbleOnLeft = focusPoint.X > workingArea.Left + (int)(workingArea.Width * 0.62);

            Point targetLocation = CalculateTargetLocation(focusPoint, workingArea);
            if (!_locationInitialized)
            {
                _displayLocation = new PointF(targetLocation.X, targetLocation.Y);
                _locationInitialized = true;
            }

            float spring = _navigationAnchorPoint.HasValue ? 0.30f : (_state == CompanionVisualState.Listening ? 0.34f : 0.24f);
            _displayLocation.X += (targetLocation.X - _displayLocation.X) * spring;
            _displayLocation.Y += (targetLocation.Y - _displayLocation.Y) * spring;

            Point nextLocation = new Point(
                (int)Math.Round(_displayLocation.X),
                (int)Math.Round(_displayLocation.Y)
            );

            if (Location != nextLocation)
            {
                Location = nextLocation;
            }

            Invalidate();
        }

        private Point CalculateTargetLocation(Point cursorPosition, Rectangle workingArea)
        {
            int desiredX = _bubbleOnLeft ? cursorPosition.X - OverlayWidth - 18 : cursorPosition.X + 18;
            int desiredY = cursorPosition.Y - OverlayHeight + 102;

            int clampedX = Math.Max(workingArea.Left + 6, Math.Min(desiredX, workingArea.Right - OverlayWidth - 6));
            int clampedY = Math.Max(workingArea.Top + 6, Math.Min(desiredY, workingArea.Bottom - OverlayHeight - 6));

            return new Point(clampedX, clampedY);
        }

        private void DrawTrail(Graphics graphics, Rectangle orbRectangle)
        {
            Color accentColor = GetAccentColor(_state);
            int direction = _bubbleOnLeft ? 1 : -1;

            for (int index = 0; index < 3; index++)
            {
                int size = 7 - index;
                int offsetX = direction * (16 + (index * 14));
                int offsetY = 10 + (index * 8);
                Rectangle trailRectangle = new Rectangle(
                    orbRectangle.X + (orbRectangle.Width / 2) + offsetX - size,
                    orbRectangle.Y + offsetY,
                    size * 2,
                    size * 2
                );

                using (SolidBrush brush = new SolidBrush(Color.FromArgb(110 - (index * 24), accentColor)))
                {
                    graphics.FillEllipse(brush, trailRectangle);
                }
            }
        }

        private void DrawBubble(Graphics graphics, Rectangle orbRectangle, string bubbleText)
        {
            const int maxTextWidth = 228;
            using (Font bubbleFont = new Font("Segoe UI Semibold", 9.5f))
            {
                SizeF measuredText = graphics.MeasureString(bubbleText, bubbleFont, maxTextWidth);
                int bubbleWidth = Math.Min(maxTextWidth + 26, Math.Max(148, (int)Math.Ceiling(measuredText.Width) + 24));
                int bubbleHeight = Math.Max(52, (int)Math.Ceiling(measuredText.Height) + 20);
                int bubbleX = _bubbleOnLeft ? Math.Max(12, orbRectangle.Left - bubbleWidth - 20) : orbRectangle.Right + 12;
                Rectangle bubbleRectangle = new Rectangle(bubbleX, 16, bubbleWidth, bubbleHeight);

                using (GraphicsPath bubblePath = CreateRoundedRectanglePath(bubbleRectangle, 16))
                using (SolidBrush bubbleShadowBrush = new SolidBrush(Color.FromArgb(82, 0, 0, 0)))
                using (SolidBrush bubbleBrush = new SolidBrush(Color.FromArgb(236, 11, 18, 31)))
                using (Pen bubbleBorderPen = new Pen(Color.FromArgb(180, GetAccentColor(_state)), 1.6f))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                using (SolidBrush pointerBrush = new SolidBrush(Color.FromArgb(220, 18, 28, 45)))
                {
                    Rectangle bubbleShadowRectangle = bubbleRectangle;
                    bubbleShadowRectangle.Offset(0, 4);
                    using (GraphicsPath shadowPath = CreateRoundedRectanglePath(bubbleShadowRectangle, 16))
                    {
                        graphics.FillPath(bubbleShadowBrush, shadowPath);
                    }

                    graphics.FillPath(bubbleBrush, bubblePath);
                    graphics.DrawPath(bubbleBorderPen, bubblePath);

                    PointF[] pointerPoints = _bubbleOnLeft
                        ? new PointF[]
                        {
                            new PointF(bubbleRectangle.Right - 2, bubbleRectangle.Bottom - 16),
                            new PointF(orbRectangle.Left + 10, orbRectangle.Top + 18),
                            new PointF(bubbleRectangle.Right - 16, bubbleRectangle.Bottom - 28)
                        }
                        : new PointF[]
                        {
                            new PointF(bubbleRectangle.Left + 2, bubbleRectangle.Bottom - 16),
                            new PointF(orbRectangle.Right - 10, orbRectangle.Top + 18),
                            new PointF(bubbleRectangle.Left + 16, bubbleRectangle.Bottom - 28)
                        };

                    graphics.FillPolygon(pointerBrush, pointerPoints);

                    RectangleF textRectangle = new RectangleF(
                        bubbleRectangle.Left + 12,
                        bubbleRectangle.Top + 10,
                        bubbleRectangle.Width - 24,
                        bubbleRectangle.Height - 16
                    );
                    graphics.DrawString(bubbleText, bubbleFont, textBrush, textRectangle);
                }
            }
        }

        private void DrawStateChip(Graphics graphics, Rectangle orbRectangle)
        {
            string stateLabel = GetStateLabel(_state);
            using (Font chipFont = new Font("Segoe UI Semibold", 8.5f))
            {
                Size chipTextSize = TextRenderer.MeasureText(stateLabel, chipFont);
                int chipWidth = Math.Max(72, chipTextSize.Width + 18);
                int chipHeight = 26;
                int chipX = _bubbleOnLeft ? orbRectangle.Right - chipWidth : orbRectangle.Left;
                int chipY = orbRectangle.Bottom + 6;
                Rectangle chipRectangle = new Rectangle(chipX, chipY, chipWidth, chipHeight);

                using (GraphicsPath chipPath = CreateRoundedRectanglePath(chipRectangle, 12))
                using (SolidBrush chipBrush = new SolidBrush(Color.FromArgb(220, 10, 18, 28)))
                using (Pen chipBorderPen = new Pen(Color.FromArgb(160, GetAccentColor(_state)), 1.2f))
                using (SolidBrush chipTextBrush = new SolidBrush(Color.White))
                {
                    graphics.FillPath(chipBrush, chipPath);
                    graphics.DrawPath(chipBorderPen, chipPath);
                    TextRenderer.DrawText(graphics, stateLabel, chipFont, chipRectangle, chipTextBrush.Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private void DrawCompanionBody(Graphics graphics, Rectangle orbRectangle)
        {
            Color accentColor = GetAccentColor(_state);
            Rectangle glowRectangle = Rectangle.Inflate(orbRectangle, 16, 16);
            Rectangle innerGlowRectangle = Rectangle.Inflate(orbRectangle, 8, 8);
            Rectangle eyeRectangle = new Rectangle(orbRectangle.Left + 19, orbRectangle.Top + 18, 32, 24);
            Rectangle pupilRectangle = new Rectangle(eyeRectangle.Left + 9, eyeRectangle.Top + 5, 12, 12);

            using (SolidBrush outerGlowBrush = new SolidBrush(Color.FromArgb(48, accentColor)))
            using (SolidBrush innerGlowBrush = new SolidBrush(Color.FromArgb(74, accentColor)))
            using (LinearGradientBrush bodyBrush = new LinearGradientBrush(orbRectangle, Color.FromArgb(255, 25, 38, 56), Color.FromArgb(255, 9, 13, 22), LinearGradientMode.ForwardDiagonal))
            using (Pen ringPen = new Pen(Color.FromArgb(185, accentColor), 2.2f))
            using (Pen innerRingPen = new Pen(Color.FromArgb(82, 255, 255, 255), 1.0f))
            using (SolidBrush eyeBrush = new SolidBrush(Color.FromArgb(248, 252, 255)))
            using (SolidBrush pupilBrush = new SolidBrush(Color.FromArgb(20, 28, 40)))
            using (SolidBrush highlightBrush = new SolidBrush(Color.FromArgb(160, 255, 255, 255)))
            {
                graphics.FillEllipse(outerGlowBrush, glowRectangle);
                graphics.FillEllipse(innerGlowBrush, innerGlowRectangle);
                graphics.FillEllipse(bodyBrush, orbRectangle);
                graphics.DrawEllipse(ringPen, orbRectangle);
                graphics.DrawEllipse(innerRingPen, Rectangle.Inflate(orbRectangle, -6, -6));

                Rectangle antennaRectangle = _bubbleOnLeft
                    ? new Rectangle(orbRectangle.Right - 10, orbRectangle.Top - 10, 10, 18)
                    : new Rectangle(orbRectangle.Left, orbRectangle.Top - 10, 10, 18);
                using (Pen antennaPen = new Pen(Color.FromArgb(150, accentColor), 2.0f))
                using (SolidBrush antennaBrush = new SolidBrush(Color.FromArgb(220, accentColor)))
                {
                    Point antennaStart = _bubbleOnLeft ? new Point(orbRectangle.Right - 14, orbRectangle.Top + 6) : new Point(orbRectangle.Left + 14, orbRectangle.Top + 6);
                    Point antennaEnd = new Point(antennaRectangle.Left + (antennaRectangle.Width / 2), antennaRectangle.Top + 8);
                    graphics.DrawLine(antennaPen, antennaStart, antennaEnd);
                    graphics.FillEllipse(antennaBrush, antennaRectangle);
                }

                graphics.FillEllipse(eyeBrush, eyeRectangle);
                graphics.FillEllipse(pupilBrush, pupilRectangle);
                graphics.FillEllipse(highlightBrush, new Rectangle(orbRectangle.Left + 18, orbRectangle.Top + 10, 14, 10));
            }

            switch (_state)
            {
                case CompanionVisualState.Listening:
                    DrawListeningIndicator(graphics, orbRectangle, accentColor);
                    break;
                case CompanionVisualState.Transcribing:
                    DrawTranscribingIndicator(graphics, orbRectangle, accentColor);
                    break;
                case CompanionVisualState.Thinking:
                    DrawThinkingIndicator(graphics, orbRectangle, accentColor);
                    break;
                case CompanionVisualState.Speaking:
                    DrawSpeakingIndicator(graphics, orbRectangle, accentColor);
                    break;
            }
        }

        private void DrawListeningIndicator(Graphics graphics, Rectangle orbRectangle, Color accentColor)
        {
            using (Pen indicatorPen = new Pen(Color.FromArgb(190, accentColor), 2.2f))
            {
                graphics.DrawArc(indicatorPen, Rectangle.Inflate(orbRectangle, 8, 8), 220, 100);
                graphics.DrawArc(indicatorPen, Rectangle.Inflate(orbRectangle, 16, 16), 218, 104);
            }
        }

        private void DrawTranscribingIndicator(Graphics graphics, Rectangle orbRectangle, Color accentColor)
        {
            using (Pen indicatorPen = new Pen(Color.FromArgb(186, accentColor), 2.0f))
            {
                indicatorPen.DashStyle = DashStyle.Dot;
                graphics.DrawArc(indicatorPen, Rectangle.Inflate(orbRectangle, 12, 12), -35, 160);
            }
        }

        private void DrawThinkingIndicator(Graphics graphics, Rectangle orbRectangle, Color accentColor)
        {
            using (SolidBrush indicatorBrush = new SolidBrush(Color.FromArgb(210, accentColor)))
            {
                int startX = orbRectangle.Left + 16;
                int y = orbRectangle.Top - 16;
                for (int index = 0; index < 3; index++)
                {
                    int size = 6 + (index == 1 ? 2 : 0);
                    graphics.FillEllipse(indicatorBrush, new Rectangle(startX + (index * 13), y + Math.Abs(index - 1) * 2, size, size));
                }
            }
        }

        private void DrawSpeakingIndicator(Graphics graphics, Rectangle orbRectangle, Color accentColor)
        {
            int direction = _bubbleOnLeft ? -1 : 1;
            int baseX = _bubbleOnLeft ? orbRectangle.Left - 6 : orbRectangle.Right + 6;
            int centerY = orbRectangle.Top + (orbRectangle.Height / 2);

            using (Pen indicatorPen = new Pen(Color.FromArgb(190, accentColor), 2.2f))
            {
                for (int index = 0; index < 3; index++)
                {
                    int width = 10 + (index * 8);
                    int height = 16 + (index * 8);
                    Rectangle waveRectangle = direction < 0
                        ? new Rectangle(baseX - width, centerY - (height / 2), width, height)
                        : new Rectangle(baseX, centerY - (height / 2), width, height);
                    graphics.DrawArc(indicatorPen, waveRectangle, direction < 0 ? 320 : 220, 80);
                }
            }
        }

        private static string GetStateLabel(CompanionVisualState state)
        {
            switch (state)
            {
                case CompanionVisualState.Listening:
                    return "listening";
                case CompanionVisualState.Transcribing:
                    return "transcribing";
                case CompanionVisualState.Thinking:
                    return "thinking";
                case CompanionVisualState.Speaking:
                    return "speaking";
                default:
                    return "ready";
            }
        }

        private static Color GetAccentColor(CompanionVisualState state)
        {
            switch (state)
            {
                case CompanionVisualState.Listening:
                    return Color.FromArgb(255, 124, 92);
                case CompanionVisualState.Transcribing:
                    return Color.FromArgb(255, 183, 77);
                case CompanionVisualState.Thinking:
                    return Color.FromArgb(88, 196, 255);
                case CompanionVisualState.Speaking:
                    return Color.FromArgb(93, 212, 136);
                default:
                    return Color.FromArgb(88, 196, 255);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly AppSettings _settings;
        private EnvironmentConfiguration _environmentConfiguration;
        private readonly List<ConversationTurn> _conversationHistory;
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _trayMenu;
        private PushToTalkHotKeyListener _hotKeyListener;
        private readonly Label _statusLabel;
        private readonly Label _hotkeyLabel;
        private readonly Label _envStatusLabel;
        private readonly TextBox _envPathTextBox;
        private readonly ComboBox _modelComboBox;
        private readonly CheckBox _speakCheckBox;
        private readonly Button _dictationButton;
        private readonly TextBox _promptTextBox;
        private readonly TextBox _responseTextBox;
        private readonly MicrophoneRecorder _microphoneRecorder;
        private CompanionOverlayForm _companionOverlay;
        private Keys _pushToTalkKey;
        private bool _quitRequested;
        private bool _isTranscribingSpeech;
        private string _currentAudioFilePath;
        private dynamic _audioPlayer;

        public MainForm()
        {
            _settings = AppSettings.Load();
            _environmentConfiguration = EnvironmentConfiguration.Load();
            _conversationHistory = new List<ConversationTurn>();
            _microphoneRecorder = new MicrophoneRecorder();

            Text = "zippy for windows";
            Width = 980;
            Height = 760;
            MinimumSize = new Size(920, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(11, 17, 27);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10.0f);
            Icon = SystemIcons.Information;

            Controls.Add(CreateLabel("zippy for windows", 24, 20, 280, 28, new Font("Segoe UI Semibold", 16.0f), Color.White));
            Controls.Add(CreateLabel("tray app + local .env + direct anthropic and elevenlabs calls", 26, 50, 620, 24, null, Color.FromArgb(160, 174, 192)));
            Controls.Add(CreateLabel(".env file", 26, 96, 120, 24));

            _envPathTextBox = CreateTextBox(26, 122, 520, 28, EnvironmentConfiguration.EnvFilePath);
            _envPathTextBox.ReadOnly = true;
            Controls.Add(_envPathTextBox);

            _envStatusLabel = CreateLabel("", 26, 154, 520, 24, null, Color.FromArgb(235, 210, 120));
            Controls.Add(_envStatusLabel);

            Controls.Add(CreateLabel("claude model", 570, 96, 120, 24));

            _modelComboBox = new ComboBox();
            _modelComboBox.Location = new Point(570, 122);
            _modelComboBox.Size = new Size(180, 28);
            _modelComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _modelComboBox.Items.Add("claude-sonnet-4-6");
            _modelComboBox.Items.Add("claude-opus-4-6");
            _modelComboBox.SelectedItem = _modelComboBox.Items.Contains(_settings.ClaudeModel) ? _settings.ClaudeModel : "claude-sonnet-4-6";
            Controls.Add(_modelComboBox);

            _speakCheckBox = new CheckBox();
            _speakCheckBox.Text = "speak responses";
            _speakCheckBox.Location = new Point(770, 123);
            _speakCheckBox.Size = new Size(150, 26);
            _speakCheckBox.Checked = _settings.SpeakResponses;
            _speakCheckBox.ForeColor = Color.White;
            _speakCheckBox.BackColor = BackColor;
            Controls.Add(_speakCheckBox);

            _statusLabel = CreateLabel("ready", 26, 188, 360, 24, null, Color.FromArgb(93, 212, 136));
            Controls.Add(_statusLabel);

            _hotkeyLabel = CreateLabel("push to talk hotkey: f8", 570, 188, 360, 24, null, Color.FromArgb(160, 174, 192));
            Controls.Add(_hotkeyLabel);

            Button saveButton = CreateButton("save settings", 26, 222, 140, 34, Color.FromArgb(33, 150, 243), Color.White);
            saveButton.Click += delegate
            {
                PersistSettings();
                SetStatus("settings saved", Color.FromArgb(93, 212, 136));
            };
            Controls.Add(saveButton);

            Button reloadEnvButton = CreateButton("reload .env", 180, 222, 140, 34, Color.FromArgb(22, 30, 45), Color.White);
            reloadEnvButton.Click += delegate
            {
                ReloadEnvironmentConfiguration();
            };
            Controls.Add(reloadEnvButton);

            Button testApiButton = CreateButton("test apis", 334, 222, 140, 34, Color.FromArgb(22, 30, 45), Color.White);
            testApiButton.Click += async delegate { await RunWorkerTestAsync(); };
            Controls.Add(testApiButton);

            Button clearHistoryButton = CreateButton("clear history", 488, 222, 140, 34, Color.FromArgb(22, 30, 45), Color.White);
            clearHistoryButton.Click += delegate
            {
                _conversationHistory.Clear();
                _responseTextBox.Clear();
                SetStatus("history cleared", Color.FromArgb(93, 212, 136));
            };
            Controls.Add(clearHistoryButton);

            Controls.Add(CreateLabel("what do you need help with?", 26, 286, 260, 24));

            _promptTextBox = CreateTextBox(26, 314, 900, 140, string.Empty);
            _promptTextBox.Multiline = true;
            _promptTextBox.AcceptsReturn = true;
            _promptTextBox.ScrollBars = ScrollBars.Vertical;
            Controls.Add(_promptTextBox);

            Button askButton = CreateButton("ask about my screen", 26, 470, 190, 38, Color.FromArgb(93, 212, 136), Color.Black);
            askButton.Click += async delegate { await RunAskFlowAsync(); };
            Controls.Add(askButton);

            _dictationButton = CreateButton("hold to talk", 230, 470, 150, 38, Color.FromArgb(22, 30, 45), Color.White);
            _dictationButton.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    HandleSpeechPress();
                }
            };
            _dictationButton.MouseUp += async delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    await HandleSpeechReleaseAsync();
                }
            };
            Controls.Add(_dictationButton);
            UpdateSpeechButtonState();

            Button copyResponseButton = CreateButton("copy response", 394, 470, 150, 38, Color.FromArgb(22, 30, 45), Color.White);
            copyResponseButton.Click += delegate
            {
                if (!string.IsNullOrWhiteSpace(_responseTextBox.Text))
                {
                    Clipboard.SetText(_responseTextBox.Text);
                    SetStatus("response copied", Color.FromArgb(93, 212, 136));
                }
            };
            Controls.Add(copyResponseButton);

            Controls.Add(CreateLabel("response", 26, 528, 160, 24));

            _responseTextBox = CreateTextBox(26, 556, 900, 132, string.Empty);
            _responseTextBox.Multiline = true;
            _responseTextBox.ReadOnly = true;
            _responseTextBox.ScrollBars = ScrollBars.Vertical;
            Controls.Add(_responseTextBox);

            Controls.Add(CreateLabel(
                "hold the button or your push-to-talk key to record. releasing transcribes your speech and sends the prompt, or routes to codex when you say nimm codex.",
                26,
                696,
                900,
                24,
                null,
                Color.FromArgb(160, 174, 192)
            ));

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = SystemIcons.Information;
            _notifyIcon.Text = "zippy for windows";
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += delegate { ShowClickyWindow(); };

            _trayMenu = new ContextMenuStrip();
            ToolStripItem openMenuItem = _trayMenu.Items.Add("Open Zippy");
            ToolStripItem askMenuItem = _trayMenu.Items.Add("Ask About Screen");
            ToolStripItem testMenuItem = _trayMenu.Items.Add("Test APIs");
            _trayMenu.Items.Add("-");
            ToolStripItem quitMenuItem = _trayMenu.Items.Add("Quit");

            openMenuItem.Click += delegate { ShowClickyWindow(); };
            askMenuItem.Click += async delegate
            {
                ShowClickyWindow();
                await RunAskFlowAsync();
            };
            testMenuItem.Click += async delegate { await RunWorkerTestAsync(); };
            quitMenuItem.Click += delegate
            {
                _quitRequested = true;
                Close();
            };
            _notifyIcon.ContextMenuStrip = _trayMenu;

            FormClosing += OnMainFormClosing;
            Shown += delegate
            {
                EnsureCompanionOverlay();
                ReloadEnvironmentConfiguration();
                SetCompanionState(CompanionVisualState.Idle);

                if (!string.IsNullOrWhiteSpace(_environmentConfiguration.Validate()))
                {
                    SetStatus("create .env first, then reload it", Color.FromArgb(235, 210, 120));
                }
                else
                {
                    _promptTextBox.Focus();
                }
            };
        }

        private async Task RunWorkerTestAsync()
        {
            try
            {
                PersistSettings();
                EnsureEnvironmentIsReady();
                SetCompanionState(CompanionVisualState.Thinking);
                SetStatus("testing apis...", Color.FromArgb(235, 210, 120));
                string responseText = await DirectApiClient.SmokeTestAsync(_settings, _environmentConfiguration);
                _responseTextBox.Text = responseText;
                SetStatus("apis ready", Color.FromArgb(93, 212, 136));
                ShowCompanionMessage(responseText, CompanionVisualState.Idle, 2600, CompanionVisualState.Idle);
            }
            catch (Exception exception)
            {
                SetStatus("api test failed", Color.FromArgb(235, 120, 120));
                ShowCompanionMessage("api test failed", CompanionVisualState.Idle, 3200, CompanionVisualState.Idle);
                MessageBox.Show(exception.Message, "Zippy api test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RunAskFlowAsync(string promptOverride = null)
        {
            string prompt = string.IsNullOrWhiteSpace(promptOverride) ? _promptTextBox.Text.Trim() : promptOverride.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("Type a question first.", "Zippy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                PersistSettings();
                EnsureEnvironmentIsReady();
                SetCompanionState(CompanionVisualState.Thinking);

                if (CodexClient.IsTriggered(prompt))
                {
                    await RunCodexFlowAsync(prompt);
                    return;
                }

                SetStatus("capturing screens...", Color.FromArgb(235, 210, 120));
                List<ScreenCaptureInfo> screenCaptures = ScreenCaptureService.CaptureAllScreens();

                SetStatus("asking zippy...", Color.FromArgb(235, 210, 120));
                string fullResponseText = await DirectApiClient.AskAsync(_settings, _environmentConfiguration, prompt, screenCaptures, _conversationHistory);
                PointTagResult pointTag = PointTagResult.Parse(fullResponseText);
                string spokenText = string.IsNullOrWhiteSpace(pointTag.SpokenText) ? fullResponseText.Trim() : pointTag.SpokenText;

                _responseTextBox.Text = spokenText;
                AddConversationTurn(prompt, spokenText);

                Point? screenPoint = ConvertPointTagToScreenPoint(pointTag, screenCaptures);
                if (screenPoint.HasValue)
                {
                    string bubbleText = !string.IsNullOrWhiteSpace(pointTag.ElementLabel) ? pointTag.ElementLabel : spokenText;
                    NavigateCompanionTo(
                        screenPoint.Value,
                        bubbleText,
                        _settings.SpeakResponses ? CompanionVisualState.Speaking : CompanionVisualState.Idle,
                        _settings.SpeakResponses ? 9000 : 7200,
                        CompanionVisualState.Idle
                    );
                }

                SetStatus("done", Color.FromArgb(93, 212, 136));
                if (!screenPoint.HasValue)
                {
                    ShowCompanionMessage(
                        spokenText,
                        _settings.SpeakResponses ? CompanionVisualState.Speaking : CompanionVisualState.Idle,
                        _settings.SpeakResponses ? 9000 : 7200,
                        CompanionVisualState.Idle
                    );
                }
                await SpeakResponseAsync(spokenText);
            }
            catch (Exception exception)
            {
                SetStatus("error", Color.FromArgb(235, 120, 120));
                ShowCompanionMessage("request failed", CompanionVisualState.Idle, 3400, CompanionVisualState.Idle);
                MessageBox.Show(exception.Message, "Zippy request failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RunCodexFlowAsync(string prompt)
        {
            List<string> temporaryImagePaths = null;
            string codexPrompt = CodexClient.RemoveTrigger(prompt);
            if (string.IsNullOrWhiteSpace(codexPrompt))
            {
                throw new InvalidOperationException("Say what Codex should do after 'nimm codex'.");
            }

            _promptTextBox.Text = codexPrompt;
            _promptTextBox.SelectionStart = _promptTextBox.TextLength;
            _promptTextBox.ScrollToCaret();

            if (CodexClient.ShouldAttachScreens(prompt))
            {
                SetStatus("capturing screens for codex...", Color.FromArgb(235, 210, 120));
                List<ScreenCaptureInfo> screenCaptures = ScreenCaptureService.CaptureAllScreens();
                temporaryImagePaths = SaveCodexScreenCaptures(screenCaptures);
            }

            SetStatus(temporaryImagePaths != null && temporaryImagePaths.Count > 0 ? "starting codex with screens..." : "starting codex...", Color.FromArgb(235, 210, 120));
            CodexRunResult result = await CodexClient.RunAsync(_environmentConfiguration, codexPrompt, temporaryImagePaths);

            string completionMessage = CodexClient.GetCompletionMessage();
            _responseTextBox.Text = completionMessage;
            SetStatus("codex done, saved to codex output", Color.FromArgb(93, 212, 136));
            ShowCompanionMessage(
                completionMessage,
                _settings.SpeakResponses ? CompanionVisualState.Speaking : CompanionVisualState.Idle,
                _settings.SpeakResponses ? 9000 : 7200,
                CompanionVisualState.Idle
            );
            await SpeakResponseAsync(completionMessage);
            DeleteFilesQuietly(temporaryImagePaths);
        }

        private static List<string> SaveCodexScreenCaptures(IList<ScreenCaptureInfo> screenCaptures)
        {
            string screenCaptureDirectory = Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..")), "codex output", "screen captures");
            Directory.CreateDirectory(screenCaptureDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            List<string> imagePaths = new List<string>();
            foreach (ScreenCaptureInfo capture in screenCaptures)
            {
                string filePath = Path.Combine(screenCaptureDirectory, string.Format("zippy-codex-screen-{0}-screen{1}.jpg", timestamp, capture.ScreenNumber));
                File.WriteAllBytes(filePath, capture.ImageBytes);
                imagePaths.Add(filePath);
            }

            return imagePaths;
        }

        private static void DeleteFilesQuietly(IList<string> filePaths)
        {
            if (filePaths == null)
            {
                return;
            }

            foreach (string filePath in filePaths)
            {
                TryDeleteFile(filePath);
            }
        }

        private void HandleSpeechPress()
        {
            if (_isTranscribingSpeech)
            {
                return;
            }

            if (_microphoneRecorder.IsRecording)
            {
                return;
            }

            EnsureCompanionOverlay();
            StartSpeechCapture();
        }

        private async Task HandleSpeechReleaseAsync()
        {
            if (_isTranscribingSpeech || !_microphoneRecorder.IsRecording)
            {
                return;
            }

            await StopSpeechCaptureAsync();
        }

        private void StartSpeechCapture()
        {
            try
            {
                _microphoneRecorder.Start();
                SetCompanionState(CompanionVisualState.Listening);
                UpdateSpeechButtonState();
                SetStatus("listening... hold the button or " + _pushToTalkKey.ToString().ToLowerInvariant() + ", then release to transcribe", Color.FromArgb(235, 210, 120));
            }
            catch (Exception exception)
            {
                SetCompanionState(CompanionVisualState.Idle);
                UpdateSpeechButtonState();
                SetStatus("microphone error", Color.FromArgb(235, 120, 120));
                ShowCompanionMessage("microphone error", CompanionVisualState.Idle, 3200, CompanionVisualState.Idle);
                MessageBox.Show(exception.Message, "Zippy microphone", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StopSpeechCaptureAsync()
        {
            string audioFilePath = null;

            try
            {
                _isTranscribingSpeech = true;
                SetCompanionState(CompanionVisualState.Transcribing);
                UpdateSpeechButtonState();

                audioFilePath = _microphoneRecorder.Stop();
                string speechToTextLabel = SpeechToTextClient.GetProviderLabel(_environmentConfiguration);
                SetStatus("transcribing with " + speechToTextLabel + "...", Color.FromArgb(235, 210, 120));

                string transcript = await SpeechToTextClient.TranscribeAsync(_environmentConfiguration, audioFilePath);
                _promptTextBox.Text = transcript;
                _promptTextBox.SelectionStart = _promptTextBox.TextLength;
                _promptTextBox.ScrollToCaret();

                SetCompanionState(CompanionVisualState.Thinking);
                SetStatus(speechToTextLabel + " ready, asking zippy...", Color.FromArgb(235, 210, 120));
                await RunAskFlowAsync(transcript);
            }
            catch (Exception exception)
            {
                SetCompanionState(CompanionVisualState.Idle);
                SetStatus("speech error", Color.FromArgb(235, 120, 120));
                ShowCompanionMessage("speech error", CompanionVisualState.Idle, 3200, CompanionVisualState.Idle);
                MessageBox.Show(exception.Message, "Zippy speech", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isTranscribingSpeech = false;
                UpdateSpeechButtonState();
                TryDeleteFile(audioFilePath);
            }
        }

        private void PersistSettings()
        {
            _settings.ClaudeModel = _modelComboBox.SelectedItem == null ? "claude-sonnet-4-6" : _modelComboBox.SelectedItem.ToString();
            _settings.SpeakResponses = _speakCheckBox.Checked;
            _settings.Save();
        }

        private void ReloadEnvironmentConfiguration()
        {
            _environmentConfiguration = EnvironmentConfiguration.Load();
            string validationError = _environmentConfiguration.Validate();
            ConfigurePushToTalkHotKey();

            if (string.IsNullOrWhiteSpace(validationError))
            {
                _envStatusLabel.Text = ".env loaded successfully";
                _envStatusLabel.ForeColor = Color.FromArgb(93, 212, 136);
            }
            else
            {
                _envStatusLabel.Text = validationError;
                _envStatusLabel.ForeColor = Color.FromArgb(235, 210, 120);
            }
        }

        private void EnsureEnvironmentIsReady()
        {
            ReloadEnvironmentConfiguration();
            string validationError = _environmentConfiguration.Validate();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                throw new InvalidOperationException(validationError + " in " + EnvironmentConfiguration.EnvFilePath);
            }
        }

        private void AddConversationTurn(string userPrompt, string responseText)
        {
            _conversationHistory.Add(new ConversationTurn
            {
                UserTranscript = userPrompt,
                AssistantResponse = responseText
            });

            while (_conversationHistory.Count > _settings.MaxConversationTurns)
            {
                _conversationHistory.RemoveAt(0);
            }
        }

        private Point? ConvertPointTagToScreenPoint(PointTagResult pointTag, IList<ScreenCaptureInfo> screenCaptures)
        {
            if (!pointTag.Coordinate.HasValue)
            {
                return null;
            }

            ScreenCaptureInfo targetCapture;
            if (pointTag.ScreenNumber.HasValue)
            {
                targetCapture = screenCaptures.FirstOrDefault(capture => capture.ScreenNumber == pointTag.ScreenNumber.Value);
            }
            else
            {
                targetCapture = screenCaptures.FirstOrDefault(capture => capture.IsCursorScreen);
            }

            if (targetCapture == null)
            {
                return null;
            }

            double clampedX = Math.Max(0, Math.Min(pointTag.Coordinate.Value.X, targetCapture.ScreenshotWidth));
            double clampedY = Math.Max(0, Math.Min(pointTag.Coordinate.Value.Y, targetCapture.ScreenshotHeight));

            double displayX = clampedX * (targetCapture.DisplayBounds.Width / (double)targetCapture.ScreenshotWidth);
            double displayY = clampedY * (targetCapture.DisplayBounds.Height / (double)targetCapture.ScreenshotHeight);

            return new Point(
                targetCapture.DisplayBounds.Left + (int)Math.Round(displayX),
                targetCapture.DisplayBounds.Top + (int)Math.Round(displayY)
            );
        }

        private async Task SpeakResponseAsync(string text)
        {
            if (!_settings.SpeakResponses || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                byte[] audioBytes = await DirectApiClient.SynthesizeSpeechAsync(_environmentConfiguration, text);
                StopAudioPlayback();
                Directory.CreateDirectory(AppSettings.StorageRoot);
                _currentAudioFilePath = Path.Combine(AppSettings.StorageRoot, "clicky-response-" + Guid.NewGuid().ToString("N") + ".mp3");
                File.WriteAllBytes(_currentAudioFilePath, audioBytes);
                PlayWithWindowsMediaPlayer(_currentAudioFilePath);
            }
            catch
            {
                SpeakLocalFallback(text);
            }
        }

        private void StopAudioPlayback()
        {
            try
            {
                if (_audioPlayer != null)
                {
                    _audioPlayer.controls.stop();
                    _audioPlayer.close();
                    _audioPlayer = null;
                }
            }
            catch
            {
                _audioPlayer = null;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(_currentAudioFilePath) && File.Exists(_currentAudioFilePath))
                {
                    File.Delete(_currentAudioFilePath);
                }
            }
            catch
            {
            }

            _currentAudioFilePath = null;
        }

        private void PlayWithWindowsMediaPlayer(string filePath)
        {
            Type mediaPlayerType = Type.GetTypeFromProgID("WMPlayer.OCX");
            if (mediaPlayerType == null)
            {
                throw new InvalidOperationException("Windows Media Player COM interface is unavailable.");
            }

            dynamic mediaPlayer = Activator.CreateInstance(mediaPlayerType);
            mediaPlayer.settings.autoStart = false;
            mediaPlayer.URL = filePath;
            mediaPlayer.controls.play();
            _audioPlayer = mediaPlayer;
        }

        private void SpeakLocalFallback(string text)
        {
            try
            {
                Type voiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (voiceType == null)
                {
                    return;
                }

                dynamic voice = Activator.CreateInstance(voiceType);
                voice.Speak(text, 1);
            }
            catch
            {
            }
        }

        private void ShowClickyWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
            _promptTextBox.Focus();
        }

        private void SetStatus(string text, Color color)
        {
            _statusLabel.Text = text;
            _statusLabel.ForeColor = color;
            Application.DoEvents();
        }

        private void EnsureCompanionOverlay()
        {
            if (_companionOverlay != null && !_companionOverlay.IsDisposed)
            {
                if (!_companionOverlay.Visible)
                {
                    _companionOverlay.Show();
                }

                return;
            }

            _companionOverlay = new CompanionOverlayForm();
            _companionOverlay.Show();
        }

        private void SetCompanionState(CompanionVisualState state)
        {
            EnsureCompanionOverlay();
            _companionOverlay.SetState(state);
        }

        private void ShowCompanionMessage(string message, CompanionVisualState state, int durationMs, CompanionVisualState stateAfterBubble)
        {
            EnsureCompanionOverlay();
            _companionOverlay.ShowMessage(message, state, durationMs, stateAfterBubble);
        }

        private void NavigateCompanionTo(Point screenPoint, string message, CompanionVisualState state, int durationMs, CompanionVisualState stateAfterNavigation)
        {
            EnsureCompanionOverlay();
            _companionOverlay.NavigateTo(screenPoint, message, state, durationMs, stateAfterNavigation);
        }

        private void UpdateSpeechButtonState()
        {
            _dictationButton.Enabled = !_isTranscribingSpeech;
            _dictationButton.Capture = _microphoneRecorder.IsRecording;

            if (_isTranscribingSpeech)
            {
                _dictationButton.Text = "transcribing...";
                _dictationButton.BackColor = Color.FromArgb(22, 30, 45);
                _dictationButton.ForeColor = Color.White;
                return;
            }

            if (_microphoneRecorder.IsRecording)
            {
                _dictationButton.Text = "release to send";
                _dictationButton.BackColor = Color.FromArgb(235, 120, 120);
                _dictationButton.ForeColor = Color.White;
                return;
            }

            _dictationButton.Text = "hold to talk";
            _dictationButton.BackColor = Color.FromArgb(22, 30, 45);
            _dictationButton.ForeColor = Color.White;
        }

        private void ConfigurePushToTalkHotKey()
        {
            Keys configuredKey = ParsePushToTalkKey(_environmentConfiguration.PushToTalkKey);

            if (_hotKeyListener != null && configuredKey == _pushToTalkKey && _hotKeyListener.IsRegistered)
            {
                _hotkeyLabel.Text = "push to talk hotkey: hold " + _pushToTalkKey.ToString().ToLowerInvariant();
                _hotkeyLabel.ForeColor = Color.FromArgb(160, 174, 192);
                return;
            }

            if (_hotKeyListener != null)
            {
                _hotKeyListener.Dispose();
                _hotKeyListener = null;
            }

            _pushToTalkKey = configuredKey;
            _hotKeyListener = new PushToTalkHotKeyListener(_pushToTalkKey);

            if (_hotKeyListener.IsRegistered)
            {
                _hotKeyListener.HotKeyPressed += delegate
                {
                    BeginInvoke((Action)delegate { HandleSpeechPress(); });
                };
                _hotKeyListener.HotKeyReleased += delegate
                {
                    BeginInvoke((Action)async delegate { await HandleSpeechReleaseAsync(); });
                };

                _hotkeyLabel.Text = "push to talk hotkey: hold " + _pushToTalkKey.ToString().ToLowerInvariant();
                _hotkeyLabel.ForeColor = Color.FromArgb(160, 174, 192);
            }
            else
            {
                _hotkeyLabel.Text = "push to talk hotkey unavailable for " + _pushToTalkKey.ToString().ToLowerInvariant();
                _hotkeyLabel.ForeColor = Color.FromArgb(235, 120, 120);
            }
        }

        private static Keys ParsePushToTalkKey(string configuredKey)
        {
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                return Keys.F8;
            }

            Keys parsedKey;
            if (Enum.TryParse(configuredKey, true, out parsedKey))
            {
                return parsedKey;
            }

            return Keys.F8;
        }

        private void OnMainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_quitRequested)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            StopAudioPlayback();
            _microphoneRecorder.Dispose();
            if (_companionOverlay != null)
            {
                _companionOverlay.Close();
                _companionOverlay.Dispose();
            }
            if (_hotKeyListener != null)
            {
                _hotKeyListener.Dispose();
            }
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _trayMenu.Dispose();
        }

        private static void TryDeleteFile(string filePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }
        }

        private static Label CreateLabel(string text, int x, int y, int width, int height, Font font = null, Color? color = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = font ?? new Font("Segoe UI", 10.0f),
                ForeColor = color ?? Color.White,
                BackColor = Color.Transparent
            };
        }

        private static TextBox CreateTextBox(int x, int y, int width, int height, string text)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                Text = text,
                BackColor = Color.FromArgb(18, 25, 38),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private static Button CreateButton(string text, int x, int y, int width, int height, Color backColor, Color foreColor)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat
            };
        }
    }
}
