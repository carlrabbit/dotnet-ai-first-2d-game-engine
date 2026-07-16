# Camera Definition and Projection Contract

## Authority

Authoritative for authored cameras, target selection, logical viewport, immediate and fixed-tick follow, dead zones, world bounds, pixel snapping, temporary offsets, shake, clipping, and camera artifacts.

Camera state changes rendering only. It never changes runtime transforms, spatial queries, collision, interaction, or targeting.

World content is camera-transformed. Screen UI is not.

Camera reconstructs from authoritative state after load. Pre-save shake does not resume.
