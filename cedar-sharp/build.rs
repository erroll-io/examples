use std::process::Command;

use uniffi_bindgen::generate_bindings;

fn main() {
    let udl_file = "./src/cedarsharp.udl";
    let config_file = "./uniffi.toml";
    let out_dir = "./bindings/";
    let crate_name: &str = "cedarsharp";

    uniffi_build::generate_scaffolding(udl_file).unwrap();
    generate_bindings(
        udl_file.into(), 
        None, 
        vec![], 
        Some(out_dir.into()), 
        None, 
        crate_name.into(),
        true).unwrap();

    Command::new("uniffi-bindgen-cs")
        .arg("--out-dir")
        .arg(out_dir)
        .arg("--config")
        .arg(config_file)
        .arg(udl_file)
        .output()
        .expect("Failed when generating C# bindings");
}
