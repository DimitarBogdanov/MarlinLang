; ModuleID = 'test'
source_filename = "test"

define i32 @__global__.Test.test(i32) {
entry:
  ret i32 0
}

define i32 @__global__.Test.main() {
entry:
  %calltmp = call i32 @__global__.Test.test(i32 5)
  ret i32 5
}
