﻿using cdcrush.lib;
using cdcrush.lib.app;
using System;
using System.IO;


namespace cdcrush.prog
{


/// <summary>
/// - Compresses a Track (data or audio)
///
/// CHANGES:
///  - track.workingFile : points to the new encoded file path
///  - track.storedFileName : is set to just a filename. e.g (track02.ogg) How it's saved in the archive?
///  - The old `track.workingFile` is deleted
/// 
/// </summary>
class TaskCompressTrack : lib.task.CTask
{
	// Point to the JOB's restore parameters
	CrushParams p;
	CueTrack track;

	string sourceTrackFile;  // Temp name, Autocalculated

	// --
	public TaskCompressTrack(CueTrack tr):base(null,"Encoding")
	{
		name = "Compress";
		desc = string.Format("Encoding track {0}", tr.trackNo);
		track = tr;
	}// -----------------------------------------

	// --
	public override void start()
	{
		base.start();

		p = (CrushParams) jobData;

		// Working file is already set and points to either TEMP or INPUT folder
		sourceTrackFile = track.workingFile;

		// Before compressing the tracks, get and store the MD5 of the track
		using(var md5 = System.Security.Cryptography.MD5.Create())
		{
			using(var str = File.OpenRead(sourceTrackFile))
			{
				track.md5 = BitConverter.ToString(md5.ComputeHash(str)).Replace("-","").ToLower();
			}
		}

		// --
		if(track.isData)
		{
			var ecm = new EcmTools(CDCRUSH.TOOLS_PATH);
			ecm.onProgress = handleProgress;
			ecm.onComplete = (s) => {
				if(s) {
					deleteOldFile();
					complete();
				}else{
					fail(msg:ecm.ERROR);
				}
			};

			// In case the task ends abruptly
			killExtra = () => ecm.kill();

			// New filename that is going to be generated:
			setupFiles(".bin.ecm");
			ecm.ecm(sourceTrackFile,track.workingFile);	// old .bin file from wherever it was to temp/bin.ecm
		}
		else // AUDIO TRACK :
		{
			var ffmp = new FFmpeg(CDCRUSH.FFMPEG_PATH);
			ffmp.onProgress = handleProgress;
			ffmp.onComplete = (s) => {
				if(s) {
					deleteOldFile();
					complete();
				}else {
					fail(msg:ffmp.ERROR);
				}
			};

			// In case the task ends abruptly
			killExtra = () => ffmp.kill();

			// Cast for easy coding
			Tuple<int,int> audioQ = jobData.audioQuality;

			// NOTE: I know this redundant, but it works :
			switch(audioQ.Item1)
			{
				case 0: // FLAC
					setupFiles(".flac");
					ffmp.audioPCMToFlac(sourceTrackFile, track.workingFile);
					break;

				case 1: // VORBIS
					setupFiles(".ogg");
					ffmp.audioPCMToOggVorbis(sourceTrackFile, audioQ.Item2, track.workingFile);
					break;

				case 2: // OPUS
					setupFiles(".ogg");
					// Opus needs an actual bitrate, not an index
					ffmp.audioPCMToOggOpus(sourceTrackFile, FFmpeg.OPUS_QUALITY[audioQ.Item2], track.workingFile);
					break;
				
				case 3: // MP3
					setupFiles(".mp3");
					ffmp.audioPCMToMP3(sourceTrackFile, audioQ.Item2, track.workingFile);
					break;

			}//- end switch

		}//- end if (track.isData)
		
	}// -----------------------------------------

	// Qucikly set :
	// + storedFileName
	// + workingFile
	void setupFiles(string ext)
	{
		track.storedFileName = track.getTrackName() + ext;

		if(p.flag_convert_only) {
			// Convert files to output folder directly
			track.workingFile = Path.Combine(jobData.outputDir, track.storedFileName);
		}else{
			// Convert files to temp folder, since they are going to be archived later
			track.workingFile = Path.Combine(jobData.tempDir, track.storedFileName);
		}
	}// -----------------------------------------

	// --
	void handleProgress(int p)
	{
		PROGRESS = p; // uses setter
	}// -----------------------------------------

	// --
	// Delete old files ONLY IF they reside in the TEMP folder!
	void deleteOldFile()
	{
		if(CDCRUSH.FLAG_KEEP_TEMP) return;

		// Make sure the file is in the TEMP folder ::
		if(jobData.flag_sourceTracksOnTemp)
		{
			File.Delete(sourceTrackFile); 
		}
	}// -----------------------------------------

}// -- end class	
}// -- end namespace
