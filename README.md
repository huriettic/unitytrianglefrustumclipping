# Unity Triangle Frustum Clipping

Triangle frustum clipping for Unity in a script

Load the scene in Unity and play, the 3 objects will be clipped to the edges of the screen.

See the clipped objects in scene view.

The frustum clipping takes the original vertices, textures, normal and triangle int to then convert the lists to triangles lists.

Then with one plane at a time of the 6 frustum planes it clips all the triangles in the mesh.

If the object's AABB is not in the frustum then it is not clipped.

There are some scripts with line segment clipping with frustum planes.

I've added a GPU parallel processing triangle clipper with custom triangle material shader.

The compute shader clips the triangles then while the buffer is on the GPU it renders the triangles.

