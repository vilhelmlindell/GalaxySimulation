#[compute]
#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 100, local_size_y = 1, local_size_z = 1) in;

struct Star {
    vec3 position;
    vec3 velocity;
    vec3 acceleration;
};

layout(set = 0, binding = 0, std430) buffer InputBuffer {
    Star stars[100];
} input_buffer;

// The code we want to execute in each invocation
void main() {
    // gl_GlobalInvocationID.x uniquely identifies this invocation across all work groups
    input_buffer.stars[gl_GlobalInvocationID.x].position.y += 0.1f;
}
