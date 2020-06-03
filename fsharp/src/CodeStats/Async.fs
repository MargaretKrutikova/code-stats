module CodeStats.Async

let map f op = 
    async {
        let! result = op
        let value = f result
        return value
    }
