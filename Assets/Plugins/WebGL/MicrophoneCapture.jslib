var MicrophoneCaptureLib = {

    $mic: {
        mediaRecorder: null,
        audioChunks:   [],
        stream:        null,
        isRecording:   false
    },

    RequestMicrophonePermission: function () {
        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function (stream) {
                // Permission granted — stop immediately, just needed the prompt
                stream.getTracks().forEach(function (t) { t.stop(); });
                console.log('[VoiceInput] Microphone permission granted.');
            })
            .catch(function (err) {
                console.error('[VoiceInput] Microphone permission denied:', err);
            });
    },

    StartWebGLRecording: function () {
        if (mic.isRecording) return;

        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function (stream) {
                mic.stream      = stream;
                mic.audioChunks = [];

                // Prefer webm/opus; fall back to whatever the browser supports
                var mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
                    ? 'audio/webm;codecs=opus'
                    : '';

                mic.mediaRecorder = mimeType
                    ? new MediaRecorder(stream, { mimeType: mimeType })
                    : new MediaRecorder(stream);

                mic.mediaRecorder.ondataavailable = function (e) {
                    if (e.data && e.data.size > 0)
                        mic.audioChunks.push(e.data);
                };

                mic.mediaRecorder.start();
                mic.isRecording = true;
                console.log('[VoiceInput] WebGL recording started.');
            })
            .catch(function (err) {
                console.error('[VoiceInput] getUserMedia error:', err);
            });
    },

    StopWebGLRecording: function (apiKeyPtr, goNamePtr, callbackPtr) {
        if (!mic.isRecording || !mic.mediaRecorder) return;

        var apiKey   = UTF8ToString(apiKeyPtr);
        var goName   = UTF8ToString(goNamePtr);
        var callback = UTF8ToString(callbackPtr);

        mic.mediaRecorder.onstop = function () {
            var mimeType = mic.mediaRecorder.mimeType || 'audio/webm';
            var blob     = new Blob(mic.audioChunks, { type: mimeType });

            var form = new FormData();
            form.append('file',     blob, 'audio.webm');
            form.append('model_id', 'scribe_v1');

            fetch('https://api.elevenlabs.io/v1/speech-to-text', {
                method:  'POST',
                headers: { 'xi-api-key': apiKey },
                body:    form
            })
            .then(function (r)    { return r.json(); })
            .then(function (data) {
                var text = (data.text || '').trim();
                console.log('[VoiceInput] Transcription:', text);
                SendMessage(goName, callback, text);
            })
            .catch(function (err) {
                console.error('[VoiceInput] STT fetch error:', err);
                SendMessage(goName, callback, '');
            });

            // Release microphone
            if (mic.stream) {
                mic.stream.getTracks().forEach(function (t) { t.stop(); });
                mic.stream = null;
            }
            mic.isRecording = false;
        };

        mic.mediaRecorder.stop();
        console.log('[VoiceInput] WebGL recording stopped.');
    }
};

autoAddDeps(MicrophoneCaptureLib, '$mic');
mergeInto(LibraryManager.library, MicrophoneCaptureLib);
