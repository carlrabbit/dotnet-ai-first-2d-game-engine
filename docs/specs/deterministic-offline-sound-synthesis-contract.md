# Deterministic Offline Sound Synthesis Contract

## Authority

Authoritative for authored synthesis definitions, oscillator and envelope vocabulary, deterministic PCM generation, WAV output, provenance, rebuilding, validation, and integration with existing sound assets.

## Boundary

Synthesis is an offline content-generation capability.

It does not:

- run on the runtime audio thread;
- alter sound-command semantics;
- affect simulation time;
- generate music or arbitrary DSP graphs;
- replace the existing sound-definition and cue-projection contracts.

Generated WAV files are ordinary raw sound assets consumed by existing sound definitions.

## Supported oscillators

```text
sine
square
triangle
noise
```

Noise generation uses an explicit deterministic seed.

## Definition fields

- schema;
- stable synthesis ID;
- output sound asset ID and relative path;
- oscillator or bounded segment sequence;
- start frequency;
- optional end frequency;
- duration;
- gain;
- attack;
- decay;
- sustain level;
- release;
- sample rate;
- deterministic seed where required;
- tags;
- provenance.

## Output

```text
container: WAV
channels: mono
sample format: signed 16-bit PCM
sample rates:
  - 22050
  - 44100
```

Definitions and generated outputs have bounded duration and amplitude.

## Determinism

Equivalent definitions, schema, and synthesis implementation version generate byte-identical PCM/WAV output.

The generated artifact records:

- definition fingerprint;
- synthesis implementation/version;
- sample count;
- sample rate;
- duration;
- peak;
- RMS summary;
- output hash;
- provenance.

## Generated-source rule

Definitions are source truth.

Generated WAV files are derived artifacts and must not be hand-edited.

Rebuild verification fails when a generated output does not match its definition.

## Runtime integration

Existing sound definitions reference generated WAV assets through ordinary raw asset references.

Physical playback remains optional and adapter-owned.
