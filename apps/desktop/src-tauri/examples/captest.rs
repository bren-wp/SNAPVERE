// Dijagnostika: hvata li xcap stvarnu sliku ili crninu na ovoj mašini?
// Ispisuje SAMO statistiku piksela (ne sprema i ne prikazuje sadržaj ekrana).
fn main() {
    let monitors = match xcap::Monitor::all() {
        Ok(m) => m,
        Err(e) => {
            println!("Monitor::all error: {e}");
            return;
        }
    };
    println!("monitora: {}", monitors.len());
    for m in monitors {
        let t = std::time::Instant::now();
        match m.capture_image() {
            Ok(img) => {
                let raw = img.as_raw();
                let n = (raw.len() / 4).max(1) as u64;
                let mut nonblack = 0u64;
                let mut sum = 0u64;
                for px in raw.chunks(4) {
                    let lum = px[0] as u64 + px[1] as u64 + px[2] as u64;
                    sum += lum;
                    if lum > 30 {
                        nonblack += 1;
                    }
                }
                println!(
                    "{}x{} | piksela={} | nonblack={} ({}%) | avg_lum={} | alpha0={} | {:?}",
                    img.width(),
                    img.height(),
                    n,
                    nonblack,
                    nonblack * 100 / n,
                    sum / n,
                    raw.chunks(4).take(1000).filter(|p| p[3] == 0).count(),
                    t.elapsed()
                );
            }
            Err(e) => println!("capture_image error: {e}"),
        }
    }
}
