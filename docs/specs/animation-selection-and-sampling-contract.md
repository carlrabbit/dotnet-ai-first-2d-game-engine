# Animation Selection and Sampling Contract

## Authority

Authoritative for base/overlay selection, selection keys, playback state, tick sampling, completion, and property composition.

## Layers

Exactly one `base` and zero or one `overlay`.

## Selection

A selection includes clip ID, selection key, and reason.

```text
same key → preserve start tick
different key → restart at current runtime tick
```

The same clip restarts only with a new key. The sampler never selects, clears, or returns clips.

## Playback

Repeat uses modulo duration and remains playing. Once samples elapsed ticks until duration, then becomes completed and samples `duration - 1`. Completed once clips hold final values.

## Sampling

Step chooses the greatest keyframe tick not exceeding local tick. Linear interpolates scalar values. After the final keyframe, hold the value.

## Composition

```text
visual defaults → base replacements → overlay replacements
```

Overlay wins conflicts. No additive, multiply, weights, masks, or cross-fades. Playback state is presentation-derived, not gameplay authority.
