[![License](https://img.shields.io/github/license/eurotools/Xbox_Adpcm_Tool)](https://www.gnu.org/licenses/gpl-3.0.html)
[![Issues](https://img.shields.io/github/issues/eurotools/Xbox_Adpcm_Tool)](https://github.com/eurotools/Xbox_Adpcm_Tool/issues)
[![GitHub Release](https://img.shields.io/github/v/release/eurotools/Xbox_Adpcm_Tool)](https://github.com/eurotools/Xbox_Adpcm_Tool/releases/latest)

# Xbox ADPCM Tool  
A command-line utility for **encoding** and **decoding** audio between **16-bit PCM WAV** and the **Xbox ADPCM** format.

This project includes a fully managed C# implementation of the Xbox ADPCM codec, featuring a block-accurate encoder and decoder compatible with the original Xbox audio format.

## Features

- Encode **16-bit PCM WAV** files into **Xbox ADPCM** format  
- Decode Xbox ADPCM data back into **16-bit PCM WAV**  
- Support for **mono** and **stereo** audio  
- Automatic RIFF/WAV header generation on output  

## Supported Input Formats

### **Encoding Input (PCM → Xbox ADPCM)**  
Your WAV file **must** meet the following conditions (validated internally):

| Requirement | Value |
|------------|--------|
| Encoding | **PCM** (uncompressed) |
| Bit Depth | **16 bits** |
| Channels | **1 (mono)** or **2 (stereo)** |
| Sample Rate | Any (22050, 44100, 48000...) |
| File Extension | `.wav` |

If these conditions are not met, the tool will display an error message  

### **Decoding Input (Xbox ADPCM → PCM)**  

The tool accepts WAV files whose payload contains Xbox ADPCM data.  
It will decode the ADPCM stream using channel count and sample rate info from the WAV header.

# Usage

This is a command-line tool.  
Open a terminal in the executable's folder and use the commands below.

### Encoding (WAV → Xbox ADPCM)

```
XboxAdpcmTool.exe input.wav output.wav
```

### Example
```
XboxAdpcmTool.exe MySound_PCM.wav MySound_ADPCM.wav
```

### Decoding (Xbox ADPCM → PCM)

```
XboxAdpcmTool.exe Decode input.wav output.wav
```

### Example
```
XboxAdpcmTool.exe Decode MySound_ADPCM.wav MySound_PCM.wav
```

## Download
Get the latest release here:

[![GitHub All Releases](https://img.shields.io/github/v/release/eurotools/Xbox_Adpcm_Tool?style=for-the-badge)](https://github.com/eurotools/Xbox_Adpcm_Tool/releases/latest)
