#[compute]
#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 512, local_size_y = 1, local_size_z = 1) in;

struct Star {
    vec4 position;
    vec4 velocity;
};

layout(set = 0, binding = 0) uniform UniformBuffer {
    float timeStep;
    float smoothingLength;
};

layout(set = 0, binding = 1, std430) buffer InputBuffer {
    Star stars[];
};

const float gravitationalConstant = 6.67430e-11; // Gravitational constant

void main() {
    uint starIndex = gl_GlobalInvocationID.x;

    Star star = stars[starIndex];
    float starMass = 1.0;

    vec4 acceleration = vec4(0);

    for (uint i = 0; i < stars.length(); ++i) {
        if (i != starIndex) {
            Star otherStar = stars[i];
            float distance = distance(star.position, otherStar.position);
            acceleration -= normalize(star.position - otherStar.position) / (distance + smoothingLength);
        }
    }

    star.velocity += acceleration * timeStep;
    star.position += star.velocity * timeStep;

    // Store the updated star back into the buffer
    stars[starIndex] = star;
}
