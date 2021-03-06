## CHANGELOG

### V1.2.4
- Progress Bar now reports total progress correctly and updates more frequently 
- Progress Report on taskbar and window title
- Detailed CD report with individual track infos and checksums
- MP3 support *(In cases you want to convert to cue/mp3 for use in specific emulators, like KEGA Fusion)*
- Small usability tweaks
- If output folder exists, program puts a "_" at the end of the folder to avoid overwriting

### V1.2.3
- Added ability to select the compression level of the final archive from 1-9
- Function to convert a `.cue/.bin` to a .cue with encoded audio files. This can be really useful as some emulators support this .cue format.
- Re-added ogg vorbis support and now user can choose between (flac, opus, vorbis). This is for when creating cue/encoded audio, some emulators might only support ogg vorbis (*like mednafen*)

### V1.2.2
- Replaced the vorbis audio codec with the **opus** codec. It seems to be a better encoder and even supports lower bitrates (*like 32k*)

### V1.2.1
- Added **FLAC** support. Audio tracks can be encoded and then restored to an exact copy of their original source, to the last byte. Now the full CD can be preserved byte for byte.

### V1.2
 - First **dotNet** release, continuing the project from the [NPM cdcrush 1.12](https://www.npmjs.com/package/cdcrush)  release
 - Calculate and display the **MD5** of the first data track, useful to quickly identify the validity of the CD
 - Ability to add an image cover to a CD archive. The image will then be displayed when restoring a CD.
 - MultiThreaded. Background FFmpeg and ECM operations can execute in parallel. *(Custom amount of threads in the options tab)*
 - Temporary files will be deleted upon user program exit.