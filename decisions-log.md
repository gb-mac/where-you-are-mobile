# Decisions Log

---

## 2026-03-17

### AR is a Companion App, Not Standalone
**Decision:** Mobile app is a companion to the desktop UE5 game, not a standalone product.
**Reasoning:** Desktop game is the primary revenue vehicle. Mobile extends engagement and drives the real-world social layer.

### Cross-Platform Item State is Shared
**Decision:** Items placed via mobile and desktop share a single authoritative backend. GPS coordinates are the common key — converted to UE5 world space via `FWYAGeoMath::GeoToWorld` on the desktop side.
**Reasoning:** Core gameplay loop requires this. A mobile player physically walks to a GPS location to drop a cache; a desktop player at the equivalent world-space position claims it.

### Tech Stack: TBD
**Decision:** Pending. Options: React Native + ARCore/ARKit, Flutter + AR plugins, Unity AR Foundation.
**Considerations:**
- React Native: large ecosystem, good GPS/maps support, AR via ViroReact or similar
- Flutter: single codebase, AR via ar_flutter_plugin
- Unity AR Foundation: easiest if team has UE5/Unity experience, native ARCore/ARKit
- **Recommendation:** Decide before any src/ code is written.
