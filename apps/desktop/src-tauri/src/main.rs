// Spriječi dodatni konzolni prozor na Windowsu u release modu.
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

fn main() {
    snapvere_lib::run();
}
