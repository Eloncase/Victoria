﻿using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Victoria.Decoder {
    /// <summary>
    /// Helper class for decoding Lavalink's <see cref="LavaTrack"/> hash.
    /// </summary>
    public readonly struct TrackDecoder {
        /// <summary>
        ///     Decodes the hash for the specified track.
        /// </summary>
        /// <param name="trackHash">Track's hash.</param>
        /// <returns>
        ///     <see cref="LavaTrack" />
        /// </returns>
        public static LavaTrack Decode(string trackHash) {
            Span<byte> hashBuffer = stackalloc byte[trackHash.Length];
            Encoding.ASCII.GetBytes(trackHash, hashBuffer);
            Base64.DecodeFromUtf8InPlace(hashBuffer, out var bytesWritten);
            var javaReader = new JavaBinaryReader(hashBuffer[..bytesWritten]);

            // Reading header
            var header = javaReader.Read<int>();
            var flags = (int) ((header & 0xC0000000L) >> 30);
            var hasVersion = (flags & 1) != 0;
            var _ = hasVersion
                ? javaReader.Read<sbyte>()
                : 1;

            var track = new LavaTrack(
                trackHash,
                title: javaReader.ReadString(),
                author: javaReader.ReadString(),
                duration: javaReader.Read<long>(),
                id: javaReader.ReadString(),
                isStream: javaReader.Read<bool>(),
                url: javaReader.Read<bool>()
                    ? javaReader.ReadString()
                    : string.Empty,
                position: default,
                canSeek: true);

            return track;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lavaConfig"></param>
        /// <param name="trackHash">Track's hash.</param>
        /// <returns>
        ///     <see cref="LavaTrack" />
        /// </returns>
        public static async Task<LavaTrack> DecodeAsync(LavaConfig lavaConfig, string trackHash) {
            if (lavaConfig == null) {
                throw new ArgumentNullException(nameof(lavaConfig));
            }

            if (string.IsNullOrWhiteSpace(trackHash)) {
                throw new ArgumentNullException(nameof(trackHash));
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{lavaConfig.HttpEndpoint}/decodetrack?track={trackHash}") {
                Headers = {
                    {"Authorization", lavaConfig.Authorization}
                }
            };

            var lavaTrack = await VictoriaExtensions.ReadAsJsonAsync<LavaTrack>(requestMessage);
            lavaTrack.Hash = trackHash;

            return lavaTrack;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lavaConfig"></param>
        /// <param name="trackHashes"></param>
        /// <returns></returns>
        public static Task<IEnumerable<LavaTrack>> DecodeAsync(LavaConfig lavaConfig,
                                                               params string[] trackHashes) {
            return DecodeAsync(lavaConfig, (IEnumerable<string>) trackHashes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lavaConfig"></param>
        /// <param name="trackHashes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Task<IEnumerable<LavaTrack>> DecodeAsync(LavaConfig lavaConfig,
                                                               IEnumerable<string> trackHashes) {
            if (lavaConfig == null) {
                throw new ArgumentNullException(nameof(lavaConfig));
            }

            var requestMessage =
                new HttpRequestMessage(HttpMethod.Post, $"{lavaConfig.HttpEndpoint}/decodetracks") {
                    Headers = {
                        {"Authorization", lavaConfig.Authorization}
                    },
                    Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(trackHashes))
                };

            return VictoriaExtensions
                .ReadAsJsonAsync<IEnumerable<LavaTrack>>(requestMessage, VictoriaExtensions.LavaTrackConverter);
        }
    }
}