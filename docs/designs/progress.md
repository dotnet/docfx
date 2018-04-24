# Progress
Progress is to provide progress bar to the end user of the CLI. The design involves the visualization style of the progress, and the API interfaces for the consumers.

## API Design

Progress: a global static object

Non-related to incremental
```cs
ProgressBar.Update();
ProgressBar.Interrupt();
ProgressBar.Tick();
ProgressBar.Complete();
```

## Typical case studies
### 1. [Mpdreamz/shellprogressbar](https://github.com/Mpdreamz/shellprogressbar)
Looks pretty cool and great for multi-thread tasks.
#### Demo
![image](https://github.com/Mpdreamz/shellprogressbar/raw/master/doc/pbar-windows.gif)

#### API usage
```cs
const int totalTicks = 10;
var options = new ProgressBarOptions
{
    ProgressCharacter = '─',
    ProgressBarOnBottom = true
};
using (var pbar = new ProgressBar(totalTicks, "Initial message", options))
{
    pbar.Tick(); //will advance pbar to 1 out of 10.
    //we can also advance and update the progressbar text
    pbar.Tick("Step 2 of 10"); 
}
```

### 2. [node-progress](https://github.com/visionmedia/node-progress)

#### Demo
```
downloading [=====             ] 39/bps 29% 3.7s
```

#### API usage
```js
var bar = new ProgressBar(':bar', {total: 10});
bar.tick();
if (bar.complete)...
bar.interrupt()
```


### 3. [verigak/progress](https://github.com/verigak/progress)

#### Demo
![image](https://camo.githubusercontent.com/2eac4822edcfe3353ad2e4b56c33b6e4b4f8955f/68747470733a2f2f7261772e6769746875622e636f6d2f7665726967616b2f70726f67726573732f6d61737465722f64656d6f2e676966)

#### API usage
```python
bar = Bar('Processing', max=20)
for i in range(20):
    # Do some work
    bar.next()
bar.finish()
```

### 4. [piotrmurach/tty-progressbar](https://github.com/piotrmurach/tty-progressbar)

#### Demo
```
# ┌ main [===============               ] 50%
# ├── one [=====          ] 34%
# └── two [==========     ] 67%
```

#### API usage
```ruby
bars = TTY::ProgressBar::Multi.new("main [:bar] :percent")

bar1 = bars.register("one [:bar] :percent", total: 15)
bar2 = bars.register("two [:bar] :percent", total: 15)

bars.start

th1 = Thread.new { 15.times { sleep(0.1); bar1.advance } }
th2 = Thread.new { 15.times { sleep(0.1); bar2.advance } }

[th1, th2].each { |t| t.join }
```

### 5. [tqdm/tqdm](https://github.com/tqdm/tqdm)

#### Demo
![image](https://raw.githubusercontent.com/tqdm/tqdm/master/images/tqdm.gif)

#### API usage
```python
pbar = tqdm(total=100)
for i in range(10):
    pbar.update(10)
pbar.close()
```

