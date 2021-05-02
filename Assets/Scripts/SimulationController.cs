using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    public ComputeShader clearShader;
    public ComputeShader screenDraw;
    public ComputeShader positionShader;
    public ComputeShader diffuseShader;

    public int clearKernel;
    public int screenKernel;
    public int positionKernel;
    public int diffuseKernel;

    public ComputeBuffer agentBuffer;

    public int numAgents = 1000000;

    public RenderTexture renderTarget;

    // Start is called before the first frame update
    void Start()
    {
        findKernels();
        renderTarget = new RenderTexture(1920,1080,24);
        renderTarget.enableRandomWrite = true;
        renderTarget.Create();
        transform.GetComponentInChildren<MeshRenderer>().material.mainTexture = renderTarget;

        clearShader.SetTexture(clearKernel,"Result",renderTarget);
        clearShader.Dispatch(clearKernel,renderTarget.width/2,renderTarget.height/2,1);

        init();
        screenDraw.SetTexture(screenKernel,"Result",renderTarget);
        screenDraw.Dispatch(screenKernel,renderTarget.width/8,renderTarget.height/8,1);
    }

    // Update is called once per frame
    void Update()
    {
        runSimulation();
    }

    void findKernels()
    {
        clearKernel = clearShader.FindKernel("SetToBlack") ;
        screenKernel = screenDraw.FindKernel("PlotAgents");
        positionKernel = positionShader.FindKernel("PosShader");
        diffuseKernel = diffuseShader.FindKernel("Diffuser");
    }

    void init()
    {
        Agent[] agents = new Agent[numAgents];
        Vector2 center = new Vector2(renderTarget.width/2,renderTarget.height/2) ;
        for(int i = 0; i < agents.Length; i++)
        {
            Vector2 startpos = center + Random.insideUnitCircle * renderTarget.height * 0.5f;
            agents[i].dir = Mathf.Atan2((center - startpos).normalized.y, (center - startpos).normalized.x);
            agents[i].pos = startpos;
            
            //agents[i].pos = center + Random.insideUnitCircle * renderTarget.height * 0.15f;
            //agents[i].dir = Random.value * Mathf.PI * 2;

            agents[i].r = 0.0f;
            agents[i].g = 255.0f;
            agents[i].b = 239.0f;
        }
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Agent));
        agentBuffer = new ComputeBuffer(agents.Length,stride);
        agentBuffer.SetData(agents);
        
        //position setup
        positionShader.SetFloat("width",renderTarget.width);
        positionShader.SetFloat("height",renderTarget.height);
        positionShader.SetInt("sensorAngle",20);
        positionShader.SetFloat("tS", 0.5f);
        positionShader.SetTexture(positionKernel,"Result",renderTarget);
        positionShader.SetBuffer(positionKernel, "agents", agentBuffer);

        //screen setup
        screenDraw.SetBuffer(screenKernel, "agents", agentBuffer);
        screenDraw.SetTexture(screenKernel, "Result", renderTarget);
        screenDraw.SetInt("numAgents", numAgents);

        //diffuse setup
        diffuseShader.SetTexture(diffuseKernel, "Result", renderTarget);
        diffuseShader.SetInt("width",renderTarget.width);
        diffuseShader.SetInt("height",renderTarget.height);
        diffuseShader.SetFloat("decay",0.75f);
    }

    void runSimulation()
    {
        //diffuse pass
        //diffuseShader.SetTexture(diffuseKernel,"Result",renderTarget);
        diffuseShader.SetFloat("dT",Time.deltaTime);
        diffuseShader.Dispatch(diffuseKernel,renderTarget.width/8,renderTarget.height/8,1);

        //position pass
        //positionShader.SetBuffer(positionKernel,"agents",agentBuffer);
        positionShader.SetFloat("dT",Time.deltaTime);
        positionShader.SetFloat("time",Time.fixedTime);
        positionShader.Dispatch(positionKernel, renderTarget.width / 8,renderTarget.height/8,1) ;

        
        //draw pass
        //screenDraw.SetBuffer(screenKernel,"agents",agentBuffer);
        //screenDraw.SetTexture(screenKernel,"Result",renderTarget);
        //screenDraw.SetInt("numAgents",numAgents);
        //screenDraw.Dispatch(screenKernel,renderTarget.width/8,renderTarget.height/8,1);


    }

    struct Agent
    {
        public Vector2 pos;
        public float dir;
        public float r;
        public float g;
        public float b;
    }

    private void onDestroy()
    {
        agentBuffer.Dispose();
    }
}
