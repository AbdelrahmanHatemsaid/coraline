## Ticket & Branch
- **Ticket ID:** `#`
- **Branch Name (`<type>/<owner>-<feature>`):**

## Summary of Changes
- 

## Pre-Merge Quality Gate
- [ ] `dotnet build` succeeds locally with **0 errors**
- [ ] `dotnet test` passes **100% of headless xUnit tests**
- [ ] All touched `.cs` files remain under **200 lines** in Newspaper Layout

## Peer Review Matrix
### If Author is Noor (Reviewed by Abdelrahman):
- [ ] `RenderSnapshot` remains strictly immutable and OpenGL-free
- [ ] Raycast LOS / A* Pathfinding edge cases verified
- [ ] Dual-World walkability rules (`RealWorldWalkable` / `OtherWorldWalkable`) respected

### If Author is Abdelrahman (Reviewed by Noor):
- [ ] `IDisposable` lifecycle implemented for all VAO/VBO/FBO/Shader/Texture handles
- [ ] Zero gameplay logic leaked into `Rendering3D`
- [ ] `GL.DebugMessageCallback` produces clean console output
