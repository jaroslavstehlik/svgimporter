//! The antialiasing fringe (DESIGN.md, 7.2): a strip along each outline of a layer that the
//! vertex shader widens to the antialiasing width on screen, centred on the outline, so shapes
//! keep their size.
//!
//! Every fringe vertex lies on the outline. Its extrusion vector is the direction the shader
//! moves it in, scaled so that both edges next to a corner move by the same distance: outward for
//! the outer edge, which is transparent (coverage 0), and inward for the inner edge, which is
//! opaque (coverage 1). Corners are miters up to a miter limit of 2 (corners down to about 60
//! degrees) and bevels beyond it. The layer's own vertices on the outline move inward by the same
//! vector, so the fill and the fringe meet without a gap or an overlap.

use std::collections::HashMap;

pub const MITER_LIMIT: f32 = 2.0;

#[derive(Default)]
pub struct Fringe {
    pub positions: Vec<[f32; 2]>,
    pub extrusions: Vec<[f32; 2]>,
    pub coverage: Vec<f32>,
    pub indices: Vec<u32>,
    /// The inward extrusion of each outline point, by the bits of its position, for the layer's
    /// own vertices.
    pub boundary: HashMap<(u32, u32), [f32; 2]>,
}

/// Builds the fringe of closed outlines whose filled area lies to the left of their direction
/// (`(-dy, dx)`), as `boundary_loops` produces them: outer boundaries with a positive signed
/// area, holes with a negative one.
pub fn build(contours: &[Vec<[f32; 2]>]) -> Fringe {
    let mut fringe = Fringe::default();
    for contour in contours {
        let count = contour.len();
        if count < 3 {
            continue;
        }
        // Per corner: the inner and outer vertex for the edge before it (a) and after it (b).
        let mut inner_a = vec![0u32; count];
        let mut outer_a = vec![0u32; count];
        let mut inner_b = vec![0u32; count];
        let mut outer_b = vec![0u32; count];

        for i in 0..count {
            let point = contour[i];
            let before = normalize(sub(point, contour[(i + count - 1) % count]));
            let after = normalize(sub(contour[(i + 1) % count], point));
            // Outward is to the right of the direction: away from the filled area.
            let normal_before = [before[1], -before[0]];
            let normal_after = [after[1], -after[0]];

            // The miter: the vector whose projection on both normals is 1.
            let denominator = 1.0 + dot(normal_before, normal_after);
            let (miter, length) = if denominator > 1e-4 {
                let m = scale(add(normal_before, normal_after), 1.0 / denominator);
                (m, len(m))
            } else {
                ([0.0, 0.0], f32::MAX)
            };
            // At sharp corners the miter is limited to its direction at the miter limit's length; a
            // path that turns back on itself is limited along its direction.
            let limited = if length > MITER_LIMIT {
                let direction = if denominator > 1e-4 { scale(miter, 1.0 / length) } else { before };
                scale(direction, MITER_LIMIT)
            } else {
                miter
            };

            if length <= MITER_LIMIT {
                let inner = fringe.add(point, neg(miter), 1.0);
                let outer = fringe.add(point, miter, 0.0);
                (inner_a[i], inner_b[i], outer_a[i], outer_b[i]) = (inner, inner, outer, outer);
            } else if dot(after, normal_before) < 0.0 {
                // The outline turns away from the outside: bevel the outer edge.
                let inner = fringe.add(point, neg(limited), 1.0);
                let (oa, ob) = (fringe.add(point, normal_before, 0.0), fringe.add(point, normal_after, 0.0));
                fringe.indices.extend_from_slice(&[inner, oa, ob]);
                (inner_a[i], inner_b[i], outer_a[i], outer_b[i]) = (inner, inner, oa, ob);
            } else {
                // The outline turns towards the outside: bevel the inner edge, and fill the gap
                // between the bevel and the layer's corner.
                let ia = fringe.add(point, neg(normal_before), 1.0);
                let ib = fringe.add(point, neg(normal_after), 1.0);
                let corner = fringe.add(point, neg(limited), 1.0);
                let outer = fringe.add(point, limited, 0.0);
                fringe.indices.extend_from_slice(&[outer, ia, ib, ia, corner, ib]);
                (inner_a[i], inner_b[i], outer_a[i], outer_b[i]) = (ia, ib, outer, outer);
            }
            fringe.boundary.insert((point[0].to_bits(), point[1].to_bits()), neg(limited));
        }

        for i in 0..count {
            let next = (i + 1) % count;
            fringe.indices.extend_from_slice(&[inner_b[i], outer_b[i], outer_a[next]]);
            fringe.indices.extend_from_slice(&[inner_b[i], outer_a[next], inner_a[next]]);
        }
    }
    fringe
}

impl Fringe {
    fn add(&mut self, point: [f32; 2], extrusion: [f32; 2], coverage: f32) -> u32 {
        self.positions.push(point);
        self.extrusions.push(extrusion);
        self.coverage.push(coverage);
        (self.positions.len() - 1) as u32
    }

    /// The inward extrusion of a layer vertex: nonzero for vertices on the outline.
    pub fn extrusion_of(&self, p: [f32; 2]) -> [f32; 2] {
        self.boundary.get(&(p[0].to_bits(), p[1].to_bits())).copied().unwrap_or([0.0, 0.0])
    }
}

fn add(a: [f32; 2], b: [f32; 2]) -> [f32; 2] {
    [a[0] + b[0], a[1] + b[1]]
}

fn sub(a: [f32; 2], b: [f32; 2]) -> [f32; 2] {
    [a[0] - b[0], a[1] - b[1]]
}

fn neg(a: [f32; 2]) -> [f32; 2] {
    [-a[0], -a[1]]
}

fn scale(a: [f32; 2], s: f32) -> [f32; 2] {
    [a[0] * s, a[1] * s]
}

fn dot(a: [f32; 2], b: [f32; 2]) -> f32 {
    a[0] * b[0] + a[1] * b[1]
}

fn len(a: [f32; 2]) -> f32 {
    dot(a, a).sqrt()
}

fn normalize(a: [f32; 2]) -> [f32; 2] {
    let l = len(a);
    if l > 0.0 { scale(a, 1.0 / l) } else { [0.0, 0.0] }
}
