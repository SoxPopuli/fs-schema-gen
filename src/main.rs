use netcorehost::nethost;

fn do_stuff(x: i32) -> bool {
    if x % 2 == 0 { return false; }


    true
}

fn main() {
    let hostfxr = nethost::load_hostfxr().unwrap();

    println!("Hello, world!");
}
